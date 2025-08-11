# Database Transactions in .NET: Entity Framework Best Practices

## Table of Contents

- [Database Transactions in .NET: Entity Framework Best Practices](#database-transactions-in-net-entity-framework-best-practices)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Key Principles](#key-principles)
  - [Anti-Patterns](#anti-patterns)
  - [Recommended Patterns](#recommended-patterns)
    - [1. Transactions Inside the Repository (Aggregate Repositories)](#1-transactions-inside-the-repository-aggregate-repositories)
    - [2. The Service Layer Pattern (Preferred)](#2-the-service-layer-pattern-preferred)
    - [3. The Unit of Work Pattern](#3-the-unit-of-work-pattern)
  - [Entity Framework Specific Patterns](#entity-framework-specific-patterns)
    - [Automatic Transaction Management](#automatic-transaction-management)
    - [Optimistic Concurrency Control](#optimistic-concurrency-control)
    - [Domain Events with Transactions](#domain-events-with-transactions)
  - [Connection Management](#connection-management)
    - [DbContext Scoping](#dbcontext-scoping)
    - [Connection Resilience](#connection-resilience)
  - [Testing Strategies](#testing-strategies)
    - [In-Memory Database for Unit Tests](#in-memory-database-for-unit-tests)
    - [Integration Tests with Real Database](#integration-tests-with-real-database)
  - [Error Handling and Monitoring](#error-handling-and-monitoring)
    - [Structured Exception Handling](#structured-exception-handling)
  - [General Advice](#general-advice)
  - [Related Topics](#related-topics)

<!-- Transformed from Go-focused documentation to .NET/Entity Framework patterns -->
<!-- REF: Based on enterprise Entity Framework usage patterns -->

## Overview

This document outlines best practices for handling database transactions in .NET
applications using Entity Framework Core, following Clean Architecture and DDD
principles.

## Key Principles

- **Atomic Operations**: Related database changes should be wrapped in transactions
- **Aggregate Boundaries**: Design transactions around aggregate roots
- **Clean Separation**: Keep business logic separate from transaction management
- **Error Handling**: Properly handle and rollback failed transactions

## Anti-Patterns

- **Skipping transactions**: Never rely on auto-commit; always use explicit transactions for related updates
- **Mixing transactions with domain logic**: Avoid passing `DbTransaction` objects through your business logic
- **One repository per table**: Design repositories around aggregates, not database tables
- **Long-running transactions**: Keep transactions as short as possible to avoid deadlocks

## Recommended Patterns

### 1. Transactions Inside the Repository (Aggregate Repositories)

Keep all data that must be consistent in a single aggregate and repository. The repository handles the transaction internally.

```csharp
public class EfOrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;
    
    public EfOrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task SaveAsync(Order order, CancellationToken cancellationToken)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Save the aggregate root
            _context.Orders.Update(order);
            
            // Save related entities within the same aggregate
            foreach (var orderItem in order.Items)
            {
                _context.OrderItems.Update(orderItem);
            }
            
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

**Pros**: Simple, aggregate boundaries are enforced
**Cons**: Business logic may leak into repositories

### 2. The Service Layer Pattern (Preferred)

Use application services to coordinate transactions across multiple repositories while keeping business logic clean.

```csharp
public class OrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IOrderRepository _orderRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IMediator _mediator;
    
    public OrderService(
        ApplicationDbContext context,
        IOrderRepository orderRepo,
        IInventoryRepository inventoryRepo,
        IMediator mediator)
    {
        _context = context;
        _orderRepo = orderRepo;
        _inventoryRepo = inventoryRepo;
        _mediator = mediator;
    }
    
    public async Task ProcessOrderAsync(ProcessOrderCommand command, CancellationToken cancellationToken)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Load aggregates
            var order = await _orderRepo.GetByIdAsync(command.OrderId, cancellationToken);
            var inventory = await _inventoryRepo.GetByProductIdAsync(command.ProductId, cancellationToken);
            
            // Execute business logic
            order.Process();
            inventory.ReserveStock(command.Quantity);
            
            // Save changes
            await _orderRepo.SaveAsync(order, cancellationToken);
            await _inventoryRepo.SaveAsync(inventory, cancellationToken);
            
            // Publish domain events
            await _mediator.PublishDomainEventsAsync(order, cancellationToken);
            
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

### 3. The Unit of Work Pattern

For complex scenarios with multiple aggregates, implement the Unit of Work pattern:

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

public class EfUnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction _transaction;
    
    public EfUnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }
    
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.CommitAsync(cancellationToken);
    }
    
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _transaction.RollbackAsync(cancellationToken);
    }
    
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}

// Usage in application service
public class TransferService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepo;
    
    public async Task TransferSubscriptionAsync(string fromUserId, string toUserId, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var fromUser = await _userRepo.GetByIdAsync(fromUserId, cancellationToken);
            var toUser = await _userRepo.GetByIdAsync(toUserId, cancellationToken);
            
            var subscription = fromUser.RemoveSubscription();
            toUser.AddSubscription(subscription);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

## Entity Framework Specific Patterns

### Automatic Transaction Management

Entity Framework automatically wraps `SaveChangesAsync` calls in transactions:

```csharp
public class SimpleRepository
{
    private readonly ApplicationDbContext _context;
    
    public async Task SaveOrderAsync(Order order, CancellationToken cancellationToken)
    {
        _context.Orders.Add(order);
        // This is automatically wrapped in a transaction
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

### Optimistic Concurrency Control

Use row versioning for optimistic concurrency:

```csharp
public class Company : IHasMetadata
{
    public string Id { get; set; }
    public string Name { get; set; }
    
    [Timestamp]
    public byte[] RowVersion { get; set; }
    
    // Or use a custom sync token approach
    public int SyncToken { get; set; }
}

// In DbContext OnModelCreating
modelBuilder.Entity<Company>(entity =>
{
    entity.Property(e => e.SyncToken).IsConcurrencyToken();
});

// Repository implementation
public async Task UpdateAsync(Company company, CancellationToken cancellationToken)
{
    try
    {
        _context.Companies.Update(company);
        await _context.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
        throw new DomainException("The company has been modified by another user");
    }
}
```

### Domain Events with Transactions

Ensure domain events are published within the same transaction:

```csharp
public class ApplicationDbContext : DbContext
{
    private readonly IMediator _mediator;
    
    public ApplicationDbContext(DbContextOptions options, IMediator mediator) : base(options)
    {
        _mediator = mediator;
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect domain events before saving
        var entities = ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();
        
        var domainEvents = entities
            .SelectMany(x => x.DomainEvents)
            .ToList();
        
        // Clear events to prevent them from being processed again
        entities.ForEach(entity => entity.ClearDomainEvents());
        
        // Save changes first
        var result = await base.SaveChangesAsync(cancellationToken);
        
        // Then publish events (within the same transaction)
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        
        return result;
    }
}
```

## Connection Management

### DbContext Scoping

Use proper DbContext scoping with dependency injection:

```csharp
// In Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString), ServiceLifetime.Scoped);

// The DI container will manage DbContext lifecycle
public class OrderController : ControllerBase
{
    private readonly ApplicationDbContext _context; // Automatically disposed
    
    public OrderController(ApplicationDbContext context)
    {
        _context = context;
    }
}
```

### Connection Resilience

Configure retry policies for connection resilience:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    }));
```

## Testing Strategies

### In-Memory Database for Unit Tests

```csharp
[Test]
public async Task OrderService_ProcessOrder_ShouldReserveInventory()
{
    // Arrange
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
    
    await using var context = new ApplicationDbContext(options);
    var orderRepo = new EfOrderRepository(context);
    var inventoryRepo = new EfInventoryRepository(context);
    var service = new OrderService(context, orderRepo, inventoryRepo, Mock.Of<IMediator>());
    
    // Add test data
    var order = Order.Create("customer1", "product1", 5);
    var inventory = Inventory.Create("product1", 10);
    
    context.Orders.Add(order);
    context.Inventory.Add(inventory);
    await context.SaveChangesAsync();
    
    // Act
    await service.ProcessOrderAsync(new ProcessOrderCommand 
    { 
        OrderId = order.Id, 
        ProductId = "product1", 
        Quantity = 5 
    }, CancellationToken.None);
    
    // Assert
    var updatedInventory = await inventoryRepo.GetByProductIdAsync("product1", CancellationToken.None);
    Assert.That(updatedInventory.AvailableStock, Is.EqualTo(5));
}
```

### Integration Tests with Real Database

```csharp
[Test]
public async Task IntegrationTest_OrderProcessing_WithRealDatabase()
{
    // Use testcontainers or a test database
    var connectionString = "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=true;";
    
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer(connectionString)
        .Options;
    
    await using var context = new ApplicationDbContext(options);
    await context.Database.EnsureCreatedAsync();
    
    // Test with real database behavior
}
```

## Error Handling and Monitoring

### Structured Exception Handling

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    
    public async Task ProcessOrderAsync(ProcessOrderCommand command, CancellationToken cancellationToken)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Processing order {OrderId}", command.OrderId);
            
            // Business logic here
            
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Successfully processed order {OrderId}", command.OrderId);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Domain validation failed for order {OrderId}: {Message}", 
                command.OrderId, ex.Message);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing order {OrderId}", command.OrderId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

## General Advice

- Design repositories around aggregates, not tables
- Keep business logic out of repositories when possible
- Use the service layer pattern for most transactional operations
- Leverage Entity Framework's built-in transaction support
- Always test under realistic concurrency and load conditions
- Use optimistic concurrency control where appropriate
- Monitor transaction performance and deadlocks

## Related Topics

- For distributed transactions (across services), see `distributed_transactions_dotnet.md`
- For Entity Framework configuration examples, see the configuration section of this guide
- For domain events, see the domain events section in `project_guidelines_dotnet.md`

---

This summary is based on .NET/Entity Framework best practices and enterprise architecture patterns.
