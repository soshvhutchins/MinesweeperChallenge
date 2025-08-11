# 🤖 GitHub Copilot Instructions for .NET Development

## Table of Contents

- [🤖 GitHub Copilot Instructions for .NET Development](#-github-copilot-instructions-for-net-development)
  - [Table of Contents](#table-of-contents)
  - [Core Development Principles](#core-development-principles)
    - [1. Clean Architecture](#1-clean-architecture)
    - [2. Domain-Driven Design (DDD)](#2-domain-driven-design-ddd)
    - [3. CQRS (Command Query Responsibility Segregation)](#3-cqrs-command-query-responsibility-segregation)
  - [Code Generation Guidelines](#code-generation-guidelines)
    - [Entity and Value Object Creation](#entity-and-value-object-creation)
    - [Command and Query Handlers](#command-and-query-handlers)
    - [Repository Pattern](#repository-pattern)
  - [Repository and Data Access](#repository-and-data-access)
    - [1. Repository Pattern Best Practices](#1-repository-pattern-best-practices)
    - [2. Entity Framework Configuration](#2-entity-framework-configuration)
    - [3. Idempotent Data Operations](#3-idempotent-data-operations)
  - [API Controller Guidelines](#api-controller-guidelines)
    - [RESTful API Design](#restful-api-design)
  - [Testing Patterns](#testing-patterns)
    - [Unit Testing Domain Logic](#unit-testing-domain-logic)
    - [Integration Testing with Entity Framework](#integration-testing-with-entity-framework)
  - [Package Preferences](#package-preferences)
  - [Anti-Patterns to Avoid](#anti-patterns-to-avoid)
  - [Project Structure](#project-structure)
  - [🔧 Configuration and Setup](#-configuration-and-setup)
    - [Program.cs Setup](#programcs-setup)
  - [📝 Code Comments and Documentation](#-code-comments-and-documentation)
  - [🔍 Error Handling](#-error-handling)
  - [⚡ Performance Considerations](#-performance-considerations)
  - [🛡️ Security Best Practices](#️-security-best-practices)
  - [🎯 Specific .NET Framework Preferences](#-specific-net-framework-preferences)

<!-- Transformed from Go-focused documentation to .NET/C# best practices -->
<!-- REF: Based on enterprise patterns and modern .NET ecosystem -->

<!-- Place inside of .github folder in project -->

## Core Development Principles

### 1. Clean Architecture

Generate code that follows Clean Architecture principles:

- **Domain Layer**: Core business logic, entities, and rules
- **Application Layer**: Use cases, service orchestration
- **Infrastructure Layer**: Entity Framework, external services, frameworks
- **Presentation Layer**: Controllers, minimal APIs, SignalR hubs

💡 **Principle**: Dependencies should always point inward. Inner layers must not depend on outer layers.

### 2. Domain-Driven Design (DDD)

When suggesting domain models:

- Use **Ubiquitous Language** that matches our business domain
- Create **Value Objects** for concepts defined by their attributes (e.g., Email, Money)
- Model **Entities** with clear identity and lifecycle
- Define **Aggregates** with clear boundaries and access through aggregate roots
- Suggest appropriate **Domain Events** for significant state changes
- Keep business logic in the domain model, not in application services

### 3. CQRS (Command Query Responsibility Segregation)

When generating application layer code:

- Separate **Commands** (state changes) from **Queries** (data retrieval)
- Use **MediatR** for implementing CQRS patterns
- Commands should not return data (except IDs for created entities)
- Queries should be side-effect free
- Use different models for reads and writes when beneficial

## Code Generation Guidelines

### Entity and Value Object Creation

When creating domain entities:

```csharp
// ✅ Good: Rich domain model
public class Order : Entity<string>
{
    public CustomerId CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }
    private readonly List<OrderItem> _items = new();
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    
    private Order() { } // EF Core constructor
    
    public static Order Create(CustomerId customerId, IEnumerable<OrderItem> items)
    {
        // Domain validation
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = customerId,
            Status = OrderStatus.Draft
        };
        
        foreach (var item in items)
        {
            order.AddItem(item);
        }
        
        return order;
    }
    
    public void AddItem(OrderItem item)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException("Cannot modify confirmed order");
        
        _items.Add(item);
        RecalculateTotal();
        RaiseDomainEvent(new ItemAddedToOrderEvent(Id, item.ProductId));
    }
}

// ✅ Value Object
public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
    
    public static Money Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new DomainException("Amount cannot be negative");
        
        return new Money(amount, currency);
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

### Command and Query Handlers

Always use MediatR patterns:

```csharp
// ✅ Command
public class CreateOrderCommand : IRequest<string>
{
    public string CustomerId { get; set; }
    public List<CreateOrderItemDto> Items { get; set; }
}

// ✅ Command Handler
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, string>
{
    private readonly IOrderRepository _orderRepo;
    private readonly ICustomerRepository _customerRepo;
    
    public CreateOrderHandler(IOrderRepository orderRepo, ICustomerRepository customerRepo)
    {
        _orderRepo = orderRepo;
        _customerRepo = customerRepo;
    }
    
    public async Task<string> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Validate customer exists
        var customer = await _customerRepo.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer == null)
            throw new NotFoundException($"Customer {request.CustomerId} not found");
        
        // Create domain objects
        var orderItems = request.Items.Select(item => 
            OrderItem.Create(item.ProductId, item.Quantity, item.UnitPrice));
        
        var order = Order.Create(CustomerId.Create(request.CustomerId), orderItems);
        
        // Save
        await _orderRepo.SaveAsync(order, cancellationToken);
        
        return order.Id;
    }
}

// ✅ Query
public class GetOrderByIdQuery : IRequest<OrderDto>
{
    public string OrderId { get; set; }
}

// ✅ Query Handler
public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepo;
    
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepo.GetByIdAsync(request.OrderId, cancellationToken);
        return order?.ToDto();
    }
}
```

### Repository Pattern

Always implement repositories following these patterns:

```csharp
// ✅ Repository Interface (in Application layer)
public interface IOrderRepository
{
    Task<Order> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken);
    Task SaveAsync(Order order, CancellationToken cancellationToken);
    Task DeleteAsync(Order order, CancellationToken cancellationToken);
}

// ✅ Repository Implementation (in Infrastructure layer)
public class EfOrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;
    
    public EfOrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Order> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }
    
    public async Task SaveAsync(Order order, CancellationToken cancellationToken)
    {
        if (await _context.Orders.AnyAsync(o => o.Id == order.Id, cancellationToken))
        {
            _context.Orders.Update(order);
        }
        else
        {
            _context.Orders.Add(order);
        }
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

## Repository and Data Access

### 1. Repository Pattern Best Practices

When generating repository code:

- Focus on domain operations, not database operations
- Abstract away Entity Framework details
- Use domain language in repository method names
- Return domain objects, not DTOs
- Handle transactions appropriately for atomic operations
- Return domain errors, not infrastructure-specific errors

### 2. Entity Framework Configuration

Always configure entities properly:

```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Customer> Customers { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ✅ Entity Configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(36);
            
            // ✅ Value Object mapping
            entity.OwnsOne(e => e.TotalAmount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("TotalAmount").HasPrecision(18, 2);
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
            
            // ✅ Collection mapping
            entity.HasMany(e => e.Items)
                  .WithOne()
                  .HasForeignKey("OrderId")
                  .OnDelete(DeleteBehavior.Cascade);
            
            // ✅ Enum mapping
            entity.Property(e => e.Status)
                  .HasConversion<string>();
        });
    }
}
```

### 3. Idempotent Data Operations

For data modification operations:

- Implement conditional checks to prevent duplicate effects
- Use upsert patterns where appropriate
- Include idempotency keys for operations that may be retried

## API Controller Guidelines

### RESTful API Design

When generating controllers:

```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<ActionResult<string>> CreateOrder(
        [FromBody] CreateOrderCommand command,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
    {
        command.IdempotencyKey = idempotencyKey;
        var orderId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetOrder), new { id = orderId }, orderId);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(string id)
    {
        var query = new GetOrderByIdQuery { OrderId = id };
        var order = await _mediator.Send(query);
        return order != null ? Ok(order) : NotFound();
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(
        string id,
        [FromBody] UpdateOrderCommand command,
        [FromHeader(Name = "If-Match")] string ifMatch)
    {
        command.Id = id;
        command.ExpectedVersion = ifMatch;
        
        try
        {
            await _mediator.Send(command);
            return NoContent();
        }
        catch (ConcurrencyException)
        {
            return Conflict("The order has been modified by another request");
        }
    }
}
```

## Testing Patterns

### Unit Testing Domain Logic

```csharp
[Test]
public void Order_AddItem_WhenDraft_ShouldAddItem()
{
    // Arrange
    var customerId = CustomerId.Create("customer-123");
    var order = Order.Create(customerId, Enumerable.Empty<OrderItem>());
    var item = OrderItem.Create("product-1", 2, Money.Create(10.00m));
    
    // Act
    order.AddItem(item);
    
    // Assert
    Assert.That(order.Items.Count, Is.EqualTo(1));
    Assert.That(order.Items.First().ProductId, Is.EqualTo("product-1"));
}

[Test]
public void Order_AddItem_WhenConfirmed_ShouldThrowException()
{
    // Arrange
    var order = CreateConfirmedOrder();
    var item = OrderItem.Create("product-1", 1, Money.Create(5.00m));
    
    // Act & Assert
    Assert.Throws<DomainException>(() => order.AddItem(item));
}
```

### Integration Testing with Entity Framework

```csharp
[Test]
public async Task OrderRepository_SaveAsync_ShouldPersistOrder()
{
    // Arrange
    using var context = CreateTestDbContext();
    var repository = new EfOrderRepository(context);
    var order = CreateValidOrder();
    
    // Act
    await repository.SaveAsync(order, CancellationToken.None);
    
    // Assert
    var savedOrder = await repository.GetByIdAsync(order.Id, CancellationToken.None);
    Assert.That(savedOrder, Is.Not.Null);
    Assert.That(savedOrder.Items.Count, Is.EqualTo(order.Items.Count));
}
```

## Package Preferences

- **Use MediatR** for CQRS implementation
- **FluentValidation** for validation
- **AutoMapper** for object mapping
- **Entity Framework Core** for data access
- **Moq** for mocking in tests
- **NUnit** as the test framework

## Anti-Patterns to Avoid

- **Avoid anemic domain models**: Models should have behavior, not just data.
- **Don't use static methods for domain logic**: Prefer instance methods to operate on domain data.
- **Avoid direct database calls in controllers**: Use services to encapsulate business logic.
- **Don't swallow exceptions**: Always handle and log exceptions appropriately.
- **Avoid magic strings/numbers**: Use constants or enums to define fixed values.

## Project Structure

Organize projects and folders by feature, not by layer:

```text
/src
  /Orders
    Orders.API
    Orders.Application
    Orders.Domain
    Orders.Infrastructure
  /Customers
    Customers.API
    Customers.Application
    Customers.Domain
    Customers.Infrastructure
/tests
  /Orders.Tests
  /Customers.Tests
```

## 🔧 Configuration and Setup

### Program.cs Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// ✅ Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Identity
builder.Services.AddDefaultIdentity<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ✅ MediatR
builder.Services.AddMediatR(typeof(CreateOrderHandler));

// ✅ Repositories
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();

// ✅ Application Services
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// ✅ FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

var app = builder.Build();

// ✅ Configure pipeline
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## 📝 Code Comments and Documentation

Always generate XML documentation for public APIs:

```csharp
/// <summary>
/// Creates a new order for the specified customer.
/// </summary>
/// <param name="command">The order creation command containing customer and item details.</param>
/// <param name="cancellationToken">Token to cancel the operation.</param>
/// <returns>The ID of the created order.</returns>
/// <exception cref="NotFoundException">Thrown when the customer does not exist.</exception>
/// <exception cref="DomainException">Thrown when domain validation fails.</exception>
public async Task<string> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
```

## 🔍 Error Handling

- Use **try-catch** blocks around code that may throw exceptions
- Log exceptions using a logging framework (e.g., Serilog, NLog)
- Return user-friendly error messages, avoid exposing internal details
- Use custom exception types for expected error conditions (e.g., NotFoundException, DomainException)

## ⚡ Performance Considerations

- Use **async/await** for I/O bound operations
- Avoid **blocking calls** like .Result or .Wait()
- Use **cancellation tokens** to allow operation cancellation
- Prefer **compiled queries** for frequently executed queries in EF Core
- Use **NoTracking** queries in EF Core for read-only data
- Optimize **value object** usage to reduce memory allocations

## 🛡️ Security Best Practices

- Use **HTTPS** for all communications
- Validate and sanitize all **input data**
- Use **parameterized queries** or ORM features to prevent SQL injection
- Implement **authentication** and **authorization** for all endpoints
- Use **Data Protection API** for sensitive data like passwords
- Regularly update dependencies to mitigate vulnerabilities

## 🎯 Specific .NET Framework Preferences

- **Use records for DTOs and simple data transfer**
- **Prefer nullable reference types**
- **Use pattern matching where appropriate**
- **Leverage C# 11+ features for concise code**
- **Use minimal APIs for simple endpoints**
- **Implement proper cancellation token support**
- **Use Entity Framework migrations for database changes**
- **Implement proper logging with ILogger**

Remember: These patterns ensure maintainable, testable, and performant .NET
applications that follow industry best practices and the Clean Architecture principles.
