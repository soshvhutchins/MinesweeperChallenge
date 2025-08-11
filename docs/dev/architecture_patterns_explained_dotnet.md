# DDD, CQRS, and Clean Architecture in `.NET/C#` - Explained Simply

## Table of Contents

- [DDD, CQRS, and Clean Architecture in `.NET/C#` - Explained Simply](#ddd-cqrs-and-clean-architecture-in-netc---explained-simply)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Domain-Driven Design (DDD)](#domain-driven-design-ddd)
    - [What is DDD?](#what-is-ddd)
    - [Key Concepts in Simple Terms](#key-concepts-in-simple-terms)
    - [`.NET/C#` Example](#netc-example)
    - [DDD Pros](#ddd-pros)
    - [DDD Cons](#ddd-cons)
  - [Command Query Responsibility Segregation (CQRS)](#command-query-responsibility-segregation-cqrs)
    - [What is CQRS?](#what-is-cqrs)
    - [How it Works in `.NET/C#`](#how-it-works-in-netc)
    - [Common .NET Implementation with MediatR (Jacopo Pattern)](#common-net-implementation-with-mediatr-jacopo-pattern)
    - [CQRS Pros](#cqrs-pros)
    - [CQRS Cons](#cqrs-cons)
  - [Clean Architecture](#clean-architecture)
    - [What is Clean Architecture?](#what-is-clean-architecture)
    - [The Layers (from inside out)](#the-layers-from-inside-out)
    - [`.NET/C#` Project Structure Example](#netc-project-structure-example)
    - [Dependency Rule](#dependency-rule)
    - [Clean Architecture Pros](#clean-architecture-pros)
    - [Clean Architecture Cons](#clean-architecture-cons)
  - [How They Work Together in ``.NET/C#``](#how-they-work-together-in-netc)
  - [When to Use These Patterns](#when-to-use-these-patterns)
    - [Use DDD When](#use-ddd-when)
    - [Use CQRS When](#use-cqrs-when)
    - [Use Clean Architecture When](#use-clean-architecture-when)
    - [Don't Use These When](#dont-use-these-when)
  - [Getting Started with ``.NET/C#``](#getting-started-with-netc)
  - [Conclusion](#conclusion)

## Overview

This document explains three popular software architecture patterns commonly used
in modern .NET applications. These patterns help organize code, make applications
more maintainable, and solve complex business problems in enterprise-scale systems.

**When to Use These Patterns**: These architectural patterns are particularly
valuable for medium to large applications with complex business logic, multiple
data sources, and requirements for maintainability and testability. They provide
structure and separation of concerns that becomes increasingly important as
applications grow.

## Domain-Driven Design (DDD)

### What is DDD?

Domain-Driven Design is like organizing your code the same way your business thinks
about itself. Instead of organizing by technical concerns (databases, web pages,
etc.), you organize by business concepts.

**Think of it like this:** If you're building software for a library, instead of
having folders called "Controllers" and "Services," you'd have folders called
"Books," "Members," and "Lending." Each folder contains everything related to that business concept.

### Key Concepts in Simple Terms

- **Domain**: The business area your software serves (e.g., banking, e-commerce, healthcare)
- **Entity**: A business object with a unique identity (like a Customer with an ID)
- **Value Object**: A business concept without identity (like an Address or Money amount)
- **Aggregate**: A group of related entities that work together (like an Order with its OrderItems)
- **Repository**: A way to save and retrieve your business objects without caring about databases
- **Domain Service**: Business logic that doesn't belong to a specific entity

### `.NET/C#` Example

```csharp
// Business domain organization (domain-focused):
// Domain/Entities/Order.cs - Order aggregate root
// Domain/Entities/Customer.cs - Customer aggregate root
// Domain/ValueObjects/EmailAddress.cs - Validated email
// Domain/ValueObjects/Money.cs - Currency and amount
// Domain/ValueObjects/OrderStatus.cs - Order state

// Instead of technical organization:
// Controllers/OrderController.cs
// Services/OrderService.cs
// Models/Order.cs
```

### DDD Pros

- ✅ Code matches how business people think
- ✅ Easier to understand for non-technical stakeholders
- ✅ Business rules are centralized and clear
- ✅ Reduces complexity in large applications
- ✅ Makes testing business logic easier

### DDD Cons

- ❌ More complex for simple applications
- ❌ Requires deep business knowledge
- ❌ Can lead to over-engineering
- ❌ Steeper learning curve
- ❌ More initial setup time

## Command Query Responsibility Segregation (CQRS)

### What is CQRS?

CQRS is like having separate lanes for reading and writing data. Instead of using
the same code path for both getting information and changing information, you create
two separate paths.

**Think of it like this:** In a library, you might have one system for checking out
books (commands/writes) and a different, faster system for searching the catalog
(queries/reads).

### How it Works in `.NET/C#`

```csharp
// Traditional approach - same model for read and write
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    // Used for both displaying and updating
}

// CQRS approach - separate models
public class CreateCustomerCommand
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class CustomerViewModel
{
    public int Id { get; set; }
    public string DisplayName { get; set; }
    public string ContactInfo { get; set; }
    // Optimized for display
}
```

### Common .NET Implementation with MediatR (Jacopo Pattern)

```csharp
### Common .NET Implementation with MediatR

```csharp
// Command (for writes) - Create Order
public record CreateOrderCommand(
    string CustomerName,
    string Email,
    List<OrderItemDto> Items) : IRequest<Result<Guid>>;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEmailService _emailService;

    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Validate business rules
        if (string.IsNullOrEmpty(request.CustomerName))
        {
            return Result<Guid>.Failure("Customer name is required");
        }

        // Create domain entity
        var order = new Order(request.CustomerName,
                            request.Email,
                            request.Items);

        // Save
        await _orderRepository.AddAsync(order, cancellationToken);

        // Side effects (email notification)
        await _emailService.SendOrderConfirmationAsync(order.Id, cancellationToken);

        return Result<Guid>.Success(order.Id);
    }
}

// Query (for reads) - Get Orders
public record GetAllOrdersQuery(PaginationQuery Pagination) : IRequest<PaginatedResult<OrderDto>>;

public class GetAllOrdersHandler : IRequestHandler<GetAllOrdersQuery, PaginatedResult<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public async Task<PaginatedResult<OrderDto>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetAllAsync(request.Pagination, cancellationToken);
        return orders.Map(order => new OrderDto(order.Id, order.CustomerName, order.Status));
    }
}
```

### CQRS Pros

- ✅ Read and write operations can be optimized separately
- ✅ Scales better for applications with many reads
- ✅ Clearer separation of concerns
- ✅ Can use different databases for reads vs writes
- ✅ Easier to add caching to read operations

### CQRS Cons

- ❌ More complex than simple CRUD operations
- ❌ Potential data consistency issues
- ❌ More code to write and maintain
- ❌ Can be overkill for simple applications
- ❌ Requires careful handling of eventual consistency

## Clean Architecture

### What is Clean Architecture?

Clean Architecture is like organizing your house so that changing the kitchen
doesn't affect the bedrooms. It's about organizing code in layers where the most
important business rules are protected from changes in external systems.

**Think of it like this:** Your business rules (like "customers must pay before
shipping") shouldn't change just because you switch from SQL Server to PostgreSQL,
or from a web app to a mobile app.

### The Layers (from inside out)

1. **Domain Layer** (Core): Your business rules and entities
2. **Application Layer**: Use cases and business workflows
3. **Infrastructure Layer**: Databases, external services, file systems
4. **Presentation Layer**: Web controllers, APIs, UI

### `.NET/C#` Project Structure Example

```text
MyApp.Domain/          (Core business logic)
├── Entities/
├── ValueObjects/
├── Interfaces/
└── Services/

MyApp.Application/     (Use cases)
├── Commands/
├── Queries/
├── Handlers/
└── Interfaces/

MyApp.Infrastructure/  (External concerns)
├── Data/
├── Services/
└── Repositories/

MyApp.Web/            (Presentation)
├── Controllers/
├── Models/
└── Views/
```

### Dependency Rule

Dependencies only point inward:

- Web layer depends on Application layer
- Application layer depends on Domain layer
- Domain layer depends on nothing

```csharp
// Good: Infrastructure depends on Domain
public class SqlCustomerRepository : ICustomerRepository // Domain interface
{
    // Implementation details
}

// Bad: Domain depending on Infrastructure
public class Customer
{
    public void Save()
    {
        var connection = new SqlConnection("..."); // Don't do this!
    }
}
```

### Clean Architecture Pros

- ✅ Business logic is independent of external systems
- ✅ Easy to test (can mock external dependencies)
- ✅ Can swap out databases, web frameworks, etc.
- ✅ Clear separation of concerns
- ✅ Follows SOLID principles

### Clean Architecture Cons

- ❌ More complex initial setup
- ❌ Can feel like over-engineering for simple apps
- ❌ Requires more interfaces and abstractions
- ❌ Steeper learning curve
- ❌ More files and projects to manage

## How They Work Together in ``.NET/C#``

These three patterns complement each other beautifully:

```csharp
// Domain Layer (DDD)
public class Order // Entity
{
    public OrderId Id { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public Money Total { get; private set; } // Value Object

    public void AddItem(Product product, Quantity quantity)
    {
        // Business logic here
    }
}

// Application Layer (CQRS + Clean Architecture)
public record PlaceOrderCommand(Guid CustomerId, List<OrderItemDto> Items) : IRequest<Guid>;

public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository; // Domain interface

    public async Task<Guid> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        // Use case logic using domain entities
        var order = new Order(new CustomerId(request.CustomerId));
        // ... business logic
        await _orderRepository.SaveAsync(order);
        return order.Id.Value;
    }
}

// Infrastructure Layer (Clean Architecture)
public class EfOrderRepository : IOrderRepository
{
    // Database implementation
}

// Presentation Layer (Clean Architecture + CQRS)
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(PlaceOrderCommand command)
    {
        var orderId = await _mediator.Send(command);
        return Ok(new { OrderId = orderId });
    }
}
```

## When to Use These Patterns

### Use DDD When

- You have complex business rules
- The business domain is the primary complexity
- You're building long-term, evolving software
- You have domain experts to work with

### Use CQRS When

- You have very different read and write patterns
- You need to optimize reads and writes separately
- You're building event-driven systems
- You have high scalability requirements

### Use Clean Architecture When

- You want to isolate business logic from external concerns
- You need to support multiple UI types (web, mobile, desktop)
- You want high testability
- You're building enterprise applications

### Don't Use These When

- Building simple CRUD applications
- Working on prototypes or short-term projects
- The team lacks experience with these patterns
- The business domain is very simple

## Getting Started with ``.NET/C#``

1. **Start Small**: Begin with Clean Architecture principles
2. **Add MediatR**: Implement CQRS gradually using MediatR
3. **Learn DDD**: Study the business domain and apply DDD concepts
4. **Use Entity Framework**: Leverage EF Core for data access
5. **Follow Examples**: Study projects like the NGWA-Business-PRO reference

## Conclusion

These architectural patterns help create maintainable, testable, and scalable
`.NET/C#` applications. They add complexity but provide significant benefits for
business-critical applications. Start simple, learn gradually, and apply these
patterns where they add value rather than complexity.

Remember: These are tools, not rules. Use them when they solve real problems in
your application.
