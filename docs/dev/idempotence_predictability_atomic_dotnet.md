# Idempotence, Predictability, and Atomic Actions in .NET

<!-- Transformed from Go-focused documentation to .NET/C# best practices -->
<!-- REF: Based on enterprise patterns and .NET ecosystem -->

## Table of Contents

- [Idempotence, Predictability, and Atomic Actions in .NET](#idempotence-predictability-and-atomic-actions-in-net)
  - [Table of Contents](#table-of-contents)
  - [What is Idempotence?](#what-is-idempotence)
  - [Implementing Idempotence in .NET with Clean Architecture and CQRS](#implementing-idempotence-in-net-with-clean-architecture-and-cqrs)
    - [Use Idempotency Keys with Command Handlers](#use-idempotency-keys-with-command-handlers)
    - [Use Conditional Operations in Repositories](#use-conditional-operations-in-repositories)
    - [Domain Event Idempotence](#domain-event-idempotence)
  - [HTTP API Idempotence](#http-api-idempotence)
    - [Controller-Level Idempotency](#controller-level-idempotency)
    - [Idempotency Middleware](#idempotency-middleware)
  - [Background Job Idempotence](#background-job-idempotence)
    - [Using Hangfire](#using-hangfire)
  - [Atomic Actions Implementation](#atomic-actions-implementation)
    - [Implementing Atomic Operations in Clean Architecture](#implementing-atomic-operations-in-clean-architecture)
  - [Predictability Through Event Sourcing](#predictability-through-event-sourcing)
  - [Testing Idempotence and Atomicity](#testing-idempotence-and-atomicity)
    - [Unit Tests for Idempotent Operations](#unit-tests-for-idempotent-operations)
    - [Integration Tests for Atomic Operations](#integration-tests-for-atomic-operations)
  - [Combining with DDD \& CQRS](#combining-with-ddd--cqrs)
  - [Implementation Checklist](#implementation-checklist)
  - [Conclusion](#conclusion)

## What is Idempotence?

Idempotence ensures that performing the same operation multiple times has the same
effect as performing it once. This is critical for building reliable distributed
systems and handling network failures, retries, and duplicate requests.

In .NET applications, idempotence becomes especially important when:

- Processing HTTP requests that might be retried
- Handling background job processing with retry mechanisms
- Implementing event-driven architectures
- Working with external API integrations

## Implementing Idempotence in .NET with Clean Architecture and CQRS

### Use Idempotency Keys with Command Handlers

This aligns with CQRS patterns using MediatR:

```csharp
public class CreateUserCommand : IRequest<string>
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string IdempotencyKey { get; set; }
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand, string>
{
    private readonly IUserRepository _userRepo;
    private readonly IIdempotencyStore _idempotencyStore;
    
    public CreateUserHandler(IUserRepository userRepo, IIdempotencyStore idempotencyStore)
    {
        _userRepo = userRepo;
        _idempotencyStore = idempotencyStore;
    }
    
    public async Task<string> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check if command was already processed using idempotency key
        var existingResult = await _idempotencyStore.GetResultAsync<string>(
            request.IdempotencyKey, cancellationToken);
        
        if (existingResult.HasValue)
        {
            return existingResult.Value; // Return previous result
        }
        
        // Domain validation within command handler
        var email = Email.Create(request.Email);
        var password = Password.Create(request.Password);
        
        // Create domain entity
        var user = User.Create(email, password);
        
        // Persist through repository
        await _userRepo.SaveAsync(user, cancellationToken);
        
        // Store result with idempotency key
        await _idempotencyStore.StoreResultAsync(
            request.IdempotencyKey, user.Id, cancellationToken);
        
        return user.Id;
    }
}
```

### Use Conditional Operations in Repositories

Implement idempotent repository save operations using Entity Framework:

```csharp
public class EfUserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    
    public EfUserRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<User> SaveAsync(User user, CancellationToken cancellationToken)
    {
        // Use upsert pattern for idempotence with Entity Framework
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);
        
        if (existingUser == null)
        {
            _context.Users.Add(user);
        }
        else
        {
            // Update only if the entity has actually changed
            _context.Entry(existingUser).CurrentValues.SetValues(user);
        }
        
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }
    
    // Alternative: Using SQL Server MERGE for true upsert
    public async Task UpsertUserAsync(User user, CancellationToken cancellationToken)
    {
        var sql = @"
            MERGE Users AS target
            USING (SELECT @Id as Id, @Email as Email, @PasswordHash as PasswordHash, @Active as Active) AS source
            ON target.Id = source.Id
            WHEN MATCHED THEN
                UPDATE SET Email = source.Email, PasswordHash = source.PasswordHash, Active = source.Active, UpdatedTime = GETUTCDATE()
            WHEN NOT MATCHED THEN
                INSERT (Id, Email, PasswordHash, Active, CreatedTime, UpdatedTime)
                VALUES (source.Id, source.Email, source.PasswordHash, source.Active, GETUTCDATE(), GETUTCDATE());";
        
        await _context.Database.ExecuteSqlRawAsync(sql,
            new SqlParameter("@Id", user.Id),
            new SqlParameter("@Email", user.Email.Value),
            new SqlParameter("@PasswordHash", user.Password.Hash),
            new SqlParameter("@Active", user.Active),
            cancellationToken);
    }
}
```

### Domain Event Idempotence

Ensure domain events are processed idempotently:

```csharp
public class UserCreatedEvent : INotification
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IEventStore _eventStore;
    
    public UserCreatedEventHandler(IEmailService emailService, IEventStore eventStore)
    {
        _emailService = emailService;
        _eventStore = eventStore;
    }
    
    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Check if event was already processed
        if (await _eventStore.WasProcessedAsync(notification.EventId, cancellationToken))
        {
            return; // Skip processing
        }
        
        // Process the event
        await _emailService.SendWelcomeEmailAsync(notification.Email, cancellationToken);
        
        // Mark as processed
        await _eventStore.MarkAsProcessedAsync(notification.EventId, cancellationToken);
    }
}
```

## HTTP API Idempotence

### Controller-Level Idempotency

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<ActionResult<string>> CreateUser(
        [FromBody] CreateUserRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
    {
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            return BadRequest("Idempotency-Key header is required");
        }
        
        var command = new CreateUserCommand
        {
            Email = request.Email,
            Password = request.Password,
            IdempotencyKey = idempotencyKey
        };
        
        var userId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetUser), new { id = userId }, userId);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(
        string id,
        [FromBody] UpdateUserRequest request,
        [FromHeader(Name = "If-Match")] string ifMatch)
    {
        // Use ETags for conditional updates
        var command = new UpdateUserCommand
        {
            Id = id,
            Email = request.Email,
            ExpectedVersion = ifMatch
        };
        
        try
        {
            await _mediator.Send(command);
            return NoContent();
        }
        catch (ConcurrencyException)
        {
            return Conflict("The user has been modified by another request");
        }
    }
}
```

### Idempotency Middleware

```csharp
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IIdempotencyStore _store;
    
    public IdempotencyMiddleware(RequestDelegate next, IIdempotencyStore store)
    {
        _next = next;
        _store = store;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsIdempotentMethod(context.Request.Method))
        {
            await _next(context);
            return;
        }
        
        var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            await _next(context);
            return;
        }
        
        // Check for existing response
        var existingResponse = await _store.GetResponseAsync(idempotencyKey);
        if (existingResponse != null)
        {
            await WriteResponseAsync(context, existingResponse);
            return;
        }
        
        // Capture response for future requests
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;
        
        await _next(context);
        
        // Store response if successful
        if (context.Response.StatusCode < 400)
        {
            var responseData = responseBodyStream.ToArray();
            await _store.StoreResponseAsync(idempotencyKey, new IdempotentResponse
            {
                StatusCode = context.Response.StatusCode,
                ContentType = context.Response.ContentType,
                Body = responseData
            });
        }
        
        // Copy response back to original stream
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalBodyStream);
    }
    
    private static bool IsIdempotentMethod(string method)
    {
        return method == "POST" || method == "PUT" || method == "PATCH";
    }
}
```

## Background Job Idempotence

### Using Hangfire

```csharp
public class EmailJobProcessor
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailJobProcessor> _logger;
    
    public EmailJobProcessor(IEmailService emailService, ILogger<EmailJobProcessor> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }
    
    [JobDisplayName("Send Welcome Email - {0}")]
    public async Task SendWelcomeEmailAsync(string userId, string idempotencyKey)
    {
        _logger.LogInformation("Processing welcome email job for user {UserId} with key {IdempotencyKey}", 
            userId, idempotencyKey);
        
        // Hangfire automatically handles retries, but we can add our own idempotency
        var jobState = $"welcome_email_{userId}";
        
        if (await _emailService.WasEmailSentAsync(jobState))
        {
            _logger.LogInformation("Welcome email already sent for user {UserId}", userId);
            return;
        }
        
        await _emailService.SendWelcomeEmailAsync(userId);
        await _emailService.MarkEmailAsSentAsync(jobState);
    }
}

// Enqueue with idempotency
public class UserService
{
    public async Task CreateUserAsync(CreateUserCommand command)
    {
        var user = await CreateUserInternalAsync(command);
        
        // Enqueue idempotent background job
        var jobId = $"welcome_email_{user.Id}";
        BackgroundJob.Enqueue<EmailJobProcessor>(
            processor => processor.SendWelcomeEmailAsync(user.Id, jobId));
    }
}
```

## Atomic Actions Implementation

### Implementing Atomic Operations in Clean Architecture

Within our layered architecture, implement atomic operations at appropriate levels:

1. **Domain Layer**: Model invariants that must be maintained atomically

   ```csharp
   public class User : Entity<string>
   {
       public Email Email { get; private set; }
       public Password Password { get; private set; }
       
       // This entire operation is conceptually atomic
       public void ChangePassword(string currentPassword, string newPassword)
       {
           if (!Password.Matches(currentPassword))
               throw new DomainException("Invalid current password");
           
           Password = Password.Create(newPassword);
           
           // Raise domain event
           RaiseDomainEvent(new PasswordChangedEvent(Id, Email.Value));
       }
   }
   ```

2. **Application Layer**: Use transactions for operations that span multiple aggregates

   ```csharp
   public class TransferSubscriptionHandler : IRequestHandler<TransferSubscriptionCommand>
   {
       private readonly ApplicationDbContext _context;
       private readonly IUserRepository _userRepo;
       
       public async Task Handle(TransferSubscriptionCommand request, CancellationToken cancellationToken)
       {
           // Begin transaction to ensure atomicity
           using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
           try
           {
               var fromUser = await _userRepo.GetByIdAsync(request.FromUserId, cancellationToken);
               var toUser = await _userRepo.GetByIdAsync(request.ToUserId, cancellationToken);
               
               // Domain logic - atomic within each aggregate
               var subscription = fromUser.RemoveSubscription();
               toUser.AddSubscription(subscription);
               
               // Persist changes
               await _userRepo.SaveAsync(fromUser, cancellationToken);
               await _userRepo.SaveAsync(toUser, cancellationToken);
               
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

## Predictability Through Event Sourcing

For complex domains requiring full auditability:

```csharp
public class UserAggregate : EventSourcedAggregate
{
    public string Id { get; private set; }
    public Email Email { get; private set; }
    public bool IsActive { get; private set; }
    
    public UserAggregate(string id)
    {
        Id = id;
    }
    
    public void Create(Email email, Password password)
    {
        if (string.IsNullOrEmpty(Id))
            throw new DomainException("User ID must be set");
        
        ApplyEvent(new UserCreatedEvent
        {
            UserId = Id,
            Email = email.Value,
            PasswordHash = password.Hash,
            OccurredAt = DateTimeOffset.UtcNow
        });
    }
    
    public void ChangeEmail(Email newEmail)
    {
        if (!IsActive)
            throw new DomainException("Cannot change email for inactive user");
        
        ApplyEvent(new EmailChangedEvent
        {
            UserId = Id,
            OldEmail = Email.Value,
            NewEmail = newEmail.Value,
            OccurredAt = DateTimeOffset.UtcNow
        });
    }
    
    // Event application methods
    private void Apply(UserCreatedEvent @event)
    {
        Id = @event.UserId;
        Email = Email.Create(@event.Email);
        IsActive = true;
    }
    
    private void Apply(EmailChangedEvent @event)
    {
        Email = Email.Create(@event.NewEmail);
    }
}
```

## Testing Idempotence and Atomicity

### Unit Tests for Idempotent Operations

```csharp
[Test]
public async Task CreateUserHandler_WithSameIdempotencyKey_ShouldReturnSameResult()
{
    // Arrange
    var handler = new CreateUserHandler(_mockUserRepo.Object, _mockIdempotencyStore.Object);
    var command = new CreateUserCommand 
    { 
        Email = "test@example.com", 
        Password = "password123",
        IdempotencyKey = "test-key-123"
    };
    
    // First execution
    var result1 = await handler.Handle(command, CancellationToken.None);
    
    // Second execution with same key
    var result2 = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.That(result1, Is.EqualTo(result2));
    _mockUserRepo.Verify(r => r.SaveAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
}
```

### Integration Tests for Atomic Operations

```csharp
[Test]
public async Task TransferSubscription_WhenSecondUserSaveFails_ShouldRollbackBothChanges()
{
    // Arrange
    using var context = CreateTestDbContext();
    var handler = new TransferSubscriptionHandler(context, new EfUserRepository(context));
    
    var fromUser = User.Create("from@example.com", "password");
    fromUser.AddSubscription(Subscription.Create("Premium"));
    var toUser = User.Create("to@example.com", "password");
    
    context.Users.AddRange(fromUser, toUser);
    await context.SaveChangesAsync();
    
    // Simulate failure during second save
    var command = new TransferSubscriptionCommand 
    { 
        FromUserId = fromUser.Id, 
        ToUserId = "invalid-id" // This will cause the operation to fail
    };
    
    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(
        () => handler.Handle(command, CancellationToken.None));
    
    // Verify rollback - fromUser should still have subscription
    var reloadedFromUser = await context.Users.FindAsync(fromUser.Id);
    Assert.That(reloadedFromUser.HasActiveSubscription(), Is.True);
}
```

## Combining with DDD & CQRS

When combined with Domain-Driven Design and CQRS:

- **Commands should be idempotent when possible**: Command handlers should detect and handle duplicate commands
- **Queries are naturally idempotent**: They don't change state, making them safe to retry
- **Domain events should capture state transitions predictably**: Include all necessary context for consumers
- **Repositories should implement idempotent save operations**: Use conditional updates or optimistic concurrency control

## Implementation Checklist

✅ **Idempotency Keys**: Implement for all state-changing operations
✅ **Atomic Transactions**: Use Entity Framework transactions for multi-aggregate operations  
✅ **Conditional Updates**: Implement upsert patterns in repositories
✅ **Event Deduplication**: Ensure domain events are processed only once
✅ **HTTP Idempotency**: Support idempotency headers in APIs
✅ **Background Job Safety**: Make all background jobs idempotent
✅ **Testing**: Verify idempotent behavior under concurrent conditions

## Conclusion

Idempotence and atomicity are essential for building reliable .NET applications.
By implementing these patterns at the domain, application, and infrastructure layers,
you create systems that are resilient to failures and safe to retry.

The key is to design for idempotence from the beginning rather than retrofitting
it later, and to test these behaviors thoroughly under realistic conditions
including concurrent access and network failures.
