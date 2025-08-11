# Distributed Transactions in .NET: Summary

## Table of Contents

- [Distributed Transactions in .NET: Summary](#distributed-transactions-in-net-summary)
  - [Table of Contents](#table-of-contents)
  - [Key Takeaways](#key-takeaways)
    - [1. Distributed Transactions Are (Almost Always) an Anti-Pattern](#1-distributed-transactions-are-almost-always-an-anti-pattern)
    - [2. The Distributed Monolith Problem](#2-the-distributed-monolith-problem)
    - [3. Eventual Consistency as a Solution](#3-eventual-consistency-as-a-solution)
    - [4. Implementation Tips in .NET](#4-implementation-tips-in-net)
      - [Using MediatR for In-Process Events](#using-mediatr-for-in-process-events)
      - [Using Message Brokers](#using-message-brokers)
    - [5. The Outbox Pattern in .NET](#5-the-outbox-pattern-in-net)
    - [6. Event Design and Coupling](#6-event-design-and-coupling)
    - [7. Testing and Monitoring in .NET](#7-testing-and-monitoring-in-net)
      - [Testing with Real Message Brokers](#testing-with-real-message-brokers)
      - [Monitoring](#monitoring)
    - [8. When to Use Distributed Transactions in .NET](#8-when-to-use-distributed-transactions-in-net)
  - [.NET-Specific Tools and Libraries](#net-specific-tools-and-libraries)
    - [Event-Driven Architecture](#event-driven-architecture)
    - [Testing](#testing)
    - [Monitoring and Observability](#monitoring-and-observability)
  - [References](#references)

---

## Key Takeaways

### 1. Distributed Transactions Are (Almost Always) an Anti-Pattern

- Avoid transactions that span multiple services or databases. They are hard to
  test, debug, and maintain, and they tightly couple your system.
- If you feel the need for distributed transactions, your service boundaries are
  likely wrong. Consider merging services or using a modular monolith.
- Even with Entity Framework's `TransactionScope` or `IDbContextTransaction`, avoid
  spanning multiple services.

### 2. The Distributed Monolith Problem

- Example: Deducting user points in one service and applying a discount in another
  can lead to inconsistencies if the second step fails.
- Synchronous HTTP calls between services (using `HttpClient`) do not guarantee atomicity.
- Entity Framework transactions only work within a single database context.

### 3. Eventual Consistency as a Solution

- Instead of trying to make everything consistent immediately, use events and
  message brokers to achieve eventual consistency.
- Publish domain events (e.g., `PointsUsedForDiscountEvent`) after updating local
  state using Entity Framework.
- Other services react to the event asynchronously using MediatR notifications or
  message brokers.
- Most of the time, the system will be consistent within milliseconds. If a service
  is down, the event will be retried until processed.

### 4. Implementation Tips in .NET

#### Using MediatR for In-Process Events

```csharp
// Domain Event
public record PointsUsedForDiscountEvent(
    Guid UserId, 
    int PointsUsed, 
    decimal DiscountAmount,
    DateTime OccurredAt) : INotification;

// Event Handler
public class PointsUsedForDiscountHandler : INotificationHandler<PointsUsedForDiscountEvent>
{
    private readonly IDiscountService _discountService;
    
    public async Task Handle(PointsUsedForDiscountEvent notification, CancellationToken cancellationToken)
    {
        await _discountService.ApplyDiscountAsync(
            notification.UserId, 
            notification.DiscountAmount, 
            cancellationToken);
    }
}

// Publishing the event
public class PointsService
{
    private readonly IMediator _mediator;
    private readonly ApplicationDbContext _context;
    
    public async Task UsePointsAsync(Guid userId, int points)
    {
        // Update local state
        var user = await _context.Users.FindAsync(userId);
        user.DeductPoints(points);
        
        await _context.SaveChangesAsync();
        
        // Publish event for other bounded contexts
        await _mediator.Publish(new PointsUsedForDiscountEvent(userId, points, CalculateDiscount(points), DateTime.UtcNow));
    }
}
```

#### Using Message Brokers

- Use message brokers like Azure Service Bus, RabbitMQ, Apache Kafka, or Redis Streams
- Libraries: MassTransit, NServiceBus, or Rebus for .NET
- Replace direct service calls with event publishing and handling

```csharp
// Using MassTransit
public class PointsService
{
    private readonly IBus _bus;
    private readonly ApplicationDbContext _context;
    
    public async Task UsePointsAsync(Guid userId, int points)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Update local state
            var user = await _context.Users.FindAsync(userId);
            user.DeductPoints(points);
            await _context.SaveChangesAsync();
            
            // Publish event
            await _bus.Publish(new PointsUsedForDiscountEvent(userId, points, CalculateDiscount(points), DateTime.UtcNow));
            
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

### 5. The Outbox Pattern in .NET

- To avoid losing events due to network issues, use the Outbox Pattern with Entity Framework:
- Store both the data change and the event in the same database transaction
- A separate background service reads events from the database and publishes them
  to the message broker

```csharp
// Outbox Event Entity
public class OutboxEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; }
    public string EventData { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
}

// Service with Outbox Pattern
public class PointsService
{
    private readonly ApplicationDbContext _context;
    private readonly IJsonSerializer _serializer;
    
    public async Task UsePointsAsync(Guid userId, int points)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Update local state
            var user = await _context.Users.FindAsync(userId);
            user.DeductPoints(points);
            
            // Store event in outbox
            var domainEvent = new PointsUsedForDiscountEvent(userId, points, CalculateDiscount(points), DateTime.UtcNow);
            var outboxEvent = new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = nameof(PointsUsedForDiscountEvent),
                EventData = _serializer.Serialize(domainEvent),
                OccurredAt = DateTime.UtcNow,
                IsProcessed = false
            };
            
            _context.OutboxEvents.Add(outboxEvent);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

// Background Service to Process Outbox
public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBus _bus;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            var unprocessedEvents = await context.OutboxEvents
                .Where(e => !e.IsProcessed)
                .OrderBy(e => e.OccurredAt)
                .Take(100)
                .ToListAsync(stoppingToken);
            
            foreach (var outboxEvent in unprocessedEvents)
            {
                try
                {
                    // Deserialize and publish event
                    var eventData = _serializer.Deserialize(outboxEvent.EventData, outboxEvent.EventType);
                    await _bus.Publish(eventData, stoppingToken);
                    
                    // Mark as processed
                    outboxEvent.IsProcessed = true;
                    outboxEvent.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    // Log error, implement retry logic
                    // Consider dead letter queues for failed events
                }
            }
            
            await context.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

### 6. Event Design and Coupling

- Events are contracts between services. Design them to state facts about your domain,
  not to expose internal workflows or intentions.
- Use record types for immutable events in C#:

```csharp
// Good: States a domain fact
public record OrderCompletedEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime CompletedAt) : INotification;

// Bad: Exposes internal workflow
public record ProcessPaymentCommand(
    Guid OrderId,
    string PaymentMethod) : INotification;
```

- Poorly designed events can create tight coupling between services.
- Use versioning strategies for event evolution (e.g., event versioning with MassTransit).

### 7. Testing and Monitoring in .NET

#### Testing with Real Message Brokers

```csharp
// Integration test using TestContainers
public class EventDrivenIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly RabbitMqContainer _rabbitMqContainer;
    
    public EventDrivenIntegrationTests()
    {
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .Build();
    }
    
    [Fact]
    public async Task UsePoints_ShouldEventuallyApplyDiscount()
    {
        // Arrange
        await _rabbitMqContainer.StartAsync();
        var connectionString = _rabbitMqContainer.GetConnectionString();
        
        // Act
        await _pointsService.UsePointsAsync(userId, 100);
        
        // Assert - Use polling to wait for eventual consistency
        await AssertEventually.ThatAsync(async () =>
        {
            var discount = await _discountService.GetDiscountAsync(userId);
            discount.Should().NotBeNull();
            discount.Amount.Should().Be(10.00m);
        }, TimeSpan.FromSeconds(10));
    }
}

// Helper for eventual consistency testing
public static class AssertEventually
{
    public static async Task ThatAsync(Func<Task> assertion, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        Exception lastException = null;
        
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await assertion();
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                await Task.Delay(100);
            }
        }
        
        throw lastException ?? new TimeoutException("Assertion timed out");
    }
}
```

#### Monitoring

- Use Application Insights or Serilog to monitor event processing
- Monitor message queue metrics (Azure Service Bus metrics, RabbitMQ management UI)
- Set up alerts for:
  - Long delays in message processing
  - Dead letter queue buildup
  - Failed event processing

```csharp
// Logging in event handlers
public class PointsUsedForDiscountHandler : INotificationHandler<PointsUsedForDiscountEvent>
{
    private readonly ILogger<PointsUsedForDiscountHandler> _logger;
    
    public async Task Handle(PointsUsedForDiscountEvent notification, CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope("Processing {EventType} for User {UserId}", 
            nameof(PointsUsedForDiscountEvent), notification.UserId);
        
        try
        {
            _logger.LogInformation("Starting discount application for {Points} points", notification.PointsUsed);
            
            await _discountService.ApplyDiscountAsync(notification.UserId, notification.DiscountAmount, cancellationToken);
            
            _logger.LogInformation("Successfully applied discount of {Amount}", notification.DiscountAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply discount for user {UserId}", notification.UserId);
            throw;
        }
    }
}
```

### 8. When to Use Distributed Transactions in .NET

- Only use true distributed transactions (System.Transactions.TransactionScope with
  MSDTC) or the Saga pattern if there is absolutely no alternative and strong
  consistency is a must.
- Consider using:
  - **Saga Pattern**: Orchestrate a series of compensating transactions
  - **Two-Phase Commit**: Only for legacy systems that require it
  - **Event Sourcing**: For complex domain logic requiring auditability

```csharp
// Saga Pattern Example with MassTransit
public class OrderProcessingSaga : MassTransitStateMachine<OrderProcessingState>
{
    public State ProcessingPayment { get; private set; }
    public State ReservingInventory { get; private set; }
    public State Completed { get; private set; }
    public State Failed { get; private set; }
    
    public Event<OrderSubmittedEvent> OrderSubmitted { get; private set; }
    public Event<PaymentProcessedEvent> PaymentProcessed { get; private set; }
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; }
    
    public OrderProcessingSaga()
    {
        Initially(
            When(OrderSubmitted)
                .TransitionTo(ProcessingPayment)
                .ThenAsync(context => context.Publish(new ProcessPaymentCommand(context.Message.OrderId))));
        
        During(ProcessingPayment,
            When(PaymentProcessed)
                .TransitionTo(ReservingInventory)
                .ThenAsync(context => context.Publish(new ReserveInventoryCommand(context.Message.OrderId))),
            When(PaymentFailed)
                .TransitionTo(Failed)
                .ThenAsync(context => context.Publish(new CancelOrderCommand(context.Message.OrderId))));
    }
}
```

- In most business cases, eventual consistency is sufficient and much simpler to implement and maintain.

---

## .NET-Specific Tools and Libraries

### Event-Driven Architecture

- **MediatR**: For in-process domain events and CQRS
- **MassTransit**: Comprehensive service bus framework
- **NServiceBus**: Enterprise messaging platform
- **Rebus**: Lightweight service bus
- **Azure Service Bus**: Cloud messaging service
- **RabbitMQ**: Open-source message broker

### Testing

- **TestContainers**: For integration testing with real message brokers
- **FluentAssertions**: For readable test assertions
- **Moq**: For mocking dependencies in unit tests
- **WebApplicationFactory**: For integration testing ASP.NET Core applications

### Monitoring and Observability

- **Application Insights**: Azure monitoring and telemetry
- **Serilog**: Structured logging library
- **OpenTelemetry**: Open-source observability framework
- **Prometheus + Grafana**: Metrics and monitoring stack

**Summary for Future .NET Projects:**

- Prefer eventual consistency and event-driven architecture over distributed transactions
- Use Entity Framework transactions only within single bounded contexts
- Implement the Outbox Pattern to ensure reliable event delivery
- Design events as domain facts using C# record types
- Use MediatR for in-process events and message brokers for cross-service communication
- Test and monitor your event-driven flows with real infrastructure
- Rethink your service boundaries if you find yourself needing distributed transactions
- Leverage .NET's strong typing and async/await patterns for robust event handling

## References

- [Distributed Transactions in Go: Read Before You Try](https://threedots.tech/post/distributed-transactions-in-go/?utm_source=newsletter&utm_medium=email&utm_term=2025-04-29&utm_campaign=Distributed+Transactions+in+Go+Read+Before+You+Try)
- [MassTransit Documentation](https://masstransit-project.com/)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Entity Framework Core Transactions](https://docs.microsoft.com/en-us/ef/core/saving/transactions)
- [Event-Driven Architecture with .NET](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
