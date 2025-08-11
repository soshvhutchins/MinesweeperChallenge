# Project Guidelines: DDD, CQRS, and Clean Architecture in .NET/C

<!-- Transformed from Go-focused documentation to .NET/C# best practices -->
<!-- REF: Based on enterprise architecture patterns -->

## Table of Contents

- [Project Guidelines: DDD, CQRS, and Clean Architecture in .NET/C](#project-guidelines-ddd-cqrs-and-clean-architecture-in-netc)
  - [Table of Contents](#table-of-contents)
  - [Introduction](#introduction)
  - [Core Concepts](#core-concepts)
    - [Domain-Driven Design (DDD)](#domain-driven-design-ddd)
      - [DDD Lite Approach](#ddd-lite-approach)
    - [CQRS (Command Query Responsibility Segregation)](#cqrs-command-query-responsibility-segregation)
      - [Practical CQRS in .NET](#practical-cqrs-in-net)
    - [Clean Architecture](#clean-architecture)
      - [Ports and Adapters (Hexagonal Architecture)](#ports-and-adapters-hexagonal-architecture)
      - [Dependency Injection](#dependency-injection)
      - [Testing Benefits](#testing-benefits)
      - [Practical .NET Advice](#practical-net-advice)
  - [Project Structure](#project-structure)
  - [Implementation Guidelines](#implementation-guidelines)
    - [Domain Layer](#domain-layer)
    - [Application Layer](#application-layer)
    - [Infrastructure Layer](#infrastructure-layer)
  - [Entity Framework and Repository Pattern](#entity-framework-and-repository-pattern)
    - [DbContext Design](#dbcontext-design)
    - [Repository Implementation Best Practices](#repository-implementation-best-practices)
  - [Controller and API Design](#controller-and-api-design)
    - [RESTful Controllers](#restful-controllers)
  - [Testing Strategy](#testing-strategy)
  - [Security Considerations](#security-considerations)
  - [Configuration and Dependency Injection](#configuration-and-dependency-injection)
  - [Conclusion](#conclusion)

## Introduction

This document provides guidelines for developing .NET applications using Domain-Driven
Design (DDD), Command Query Responsibility Segregation (CQRS), and Clean Architecture
principles. These approaches combine to create maintainable, testable, and
business-focused code using modern .NET practices.

## Core Concepts

### Domain-Driven Design (DDD)

DDD focuses on creating a software model that reflects the business domain:

- **Ubiquitous Language**: Use consistent terminology between developers and domain experts
- **Bounded Contexts**: Divide the domain into distinct areas with clear boundaries
- **Aggregates**: Treat related entities as a single unit with a root entity
- **Domain Events**: Model significant occurrences within the domain
- **Value Objects**: Immutable objects defined by their attributes
- **Entities**: Objects with identity that persists across state changes
- **Repositories**: Abstractions for persisting and retrieving domain objects

#### DDD Lite Approach

Following a pragmatic version of DDD that focuses on the most valuable aspects:

- **Focus on bounded contexts first**: Clearly define boundaries between different parts of the system
- **Identify aggregates**: Group related entities, but keep aggregates small and focused
- **Use value objects**: Encapsulate validation and business rules in value objects
- **Apply tactical patterns selectively**: Use repositories, domain services, and entities where they provide clear value

### CQRS (Command Query Responsibility Segregation)

CQRS separates operations that read data from operations that write data:

- **Commands**: Operations that change state but don't return data
- **Queries**: Operations that return data but don't change state
- **Command Handlers**: Process commands and update the domain model
- **Query Handlers**: Process queries and return data representations

#### Practical CQRS in .NET

- **Keep it simple:** You don't need a framework—CQRS can be implemented with C#
  classes and interfaces
- **Command/Query separation:** Define separate types and handlers for commands
  and queries
- **Handler interfaces:** Use interfaces like `IRequestHandler<TCommand>` for
  commands and `IRequestHandler<TQuery, TResult>` for queries
- **Decouple side effects:** Command handlers should encapsulate side effects
  (e.g., database writes, publishing events), while query handlers should be
  side-effect free
- **Testing:** CQRS makes it easier to test business logic in isolation

**Example:**

```csharp
// Command
public class CreateUserCommand : IRequest<string>
{
    public string Email { get; set; }
    public string Password { get; set; }
}

// Command Handler
public class CreateUserHandler : IRequestHandler<CreateUserCommand, string>
{
    private readonly IUserRepository _repo;
    
    public CreateUserHandler(IUserRepository repo)
    {
        _repo = repo;
    }
    
    public async Task<string> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Validate, hash password, etc.
        var user = User.Create(request.Email, request.Password);
        await _repo.SaveAsync(user, cancellationToken);
        return user.Id;
    }
}

// Query
public class GetUserByEmailQuery : IRequest<User>
{
    public string Email { get; set; }
}

// Query Handler
public class GetUserByEmailHandler : IRequestHandler<GetUserByEmailQuery, User>
{
    private readonly IUserRepository _repo;
    
    public GetUserByEmailHandler(IUserRepository repo)
    {
        _repo = repo;
    }
    
    public async Task<User> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
    {
        return await _repo.FindByEmailAsync(request.Email, cancellationToken);
    }
}
```

- **Use MediatR:** For .NET projects, MediatR provides excellent infrastructure
  for implementing CQRS patterns
- **Keep models simple:** You can use the same domain models for both commands and
  queries unless you have specific performance or security needs

### Clean Architecture

Clean Architecture organizes code in concentric layers, with dependencies always
pointing inward:

- **Dependency Rule:** Code in outer layers can depend on inner layers, but never
  the other way around
- **Domain Layer:** Pure business logic, entities, value objects, and domain services.
  No dependencies on frameworks or infrastructure
- **Application Layer:** Orchestrates use cases, coordinates domain objects, and
  defines interfaces (ports) for infrastructure
- **Infrastructure Layer:** Frameworks, databases, external services, and technical
  details. Implements interfaces (ports) defined in the application layer

#### Ports and Adapters (Hexagonal Architecture)

Use the Ports and Adapters pattern to decouple the core from the outside world:

- **Ports:** Interfaces defined by the application layer (e.g., repository interfaces, service interfaces)
- **Adapters:** Implementations of those interfaces (e.g., Entity Framework repository, Web API controller)

#### Dependency Injection

Dependencies should be injected via constructor injection:

```csharp
public class UserService
{
    private readonly IUserRepository _repo;
    
    public UserService(IUserRepository repo)
    {
        _repo = repo;
    }
}
```

#### Testing Benefits

Clean Architecture makes it easy to test business logic in isolation, as the domain
and application layers have no dependencies on frameworks or databases.

#### Practical .NET Advice

- Start simple; only introduce more layers and abstractions as the project grows
- Use .NET interfaces to invert dependencies, but avoid unnecessary abstractions
- Leverage built-in dependency injection container

## Project Structure

```sh
/src
  /Domain                    # Domain model, entities, value objects, domain services
    /Entities               # Domain entities
    /ValueObjects           # Value objects and enums
    /Services               # Domain services
    /Events                 # Domain events
  /Application              # Application services, commands, queries, handlers
    /Commands               # Command definitions and handlers
    /Queries                # Query definitions and handlers
    /Services               # Application services
    /Interfaces             # Application interfaces (ports)
  /Infrastructure           # Infrastructure concerns, Entity Framework, external services
    /Data                   # Entity Framework DbContext and configurations
    /Repositories           # Repository implementations
    /Services               # External service implementations
  /Presentation             # Controllers, SignalR hubs, minimal APIs
    /Controllers            # Web API controllers
    /Areas                  # MVC areas for organization
/tests                      # Test projects
/docs                       # Documentation
```

## Implementation Guidelines

### Domain Layer

1. **Start with the domain model**:

   ```csharp
   // Example of a domain entity
   public class User : Entity<string>
   {
       public Email Email { get; private set; }     // Value object
       public Password Password { get; private set; } // Value object
       public UserRole Role { get; private set; }     // Enum
       public bool Active { get; private set; }
       
       private User() { } // EF Core constructor
       
       public static User Create(string email, string password)
       {
           var emailVo = Email.Create(email);
           var passwordVo = Password.Create(password);
           
           return new User
           {
               Id = Guid.NewGuid().ToString(),
               Email = emailVo,
               Password = passwordVo,
               Role = UserRole.User,
               Active = true
           };
       }
       
       // Domain logic within entity
       public void ChangePassword(string currentPassword, string newPassword)
       {
           if (!Password.Matches(currentPassword))
               throw new DomainException("Invalid current password");
           
           Password = Password.Create(newPassword);
       }
   }
   ```

2. **Use value objects for validation**:

   ```csharp
   public class Email : ValueObject
   {
       public string Value { get; private set; }
       
       private Email(string value)
       {
           Value = value;
       }
       
       public static Email Create(string email)
       {
           if (string.IsNullOrWhiteSpace(email))
               throw new DomainException("Email cannot be empty");
           
           if (!IsValidEmail(email))
               throw new DomainException("Invalid email format");
           
           return new Email(email);
       }
       
       protected override IEnumerable<object> GetEqualityComponents()
       {
           yield return Value;
       }
       
       private static bool IsValidEmail(string email)
       {
           // Email validation logic
           return email.Contains("@");
       }
   }
   ```

3. **Keep business logic in the domain**:

   ```csharp
   // Good: Domain logic in the domain model
   public class Training : Entity<string>
   {
       public bool CanBeAttendedBy(User user)
       {
           if (!IsActive())
               throw new DomainException("Training is not active");
           
           if (!user.HasActiveSubscription())
               throw new DomainException("User has no active subscription");
           
           return true;
       }
   }
   ```

### Application Layer

1. **Define commands and queries**:

   ```csharp
   // Command
   public class CreateUserCommand : IRequest<string>
   {
       public string Email { get; set; }
       public string Password { get; set; }
   }
   
   // Query
   public class GetUserByIdQuery : IRequest<UserDto>
   {
       public string Id { get; set; }
   }
   ```

2. **Implement command/query handlers**:

   ```csharp
   // Command handler
   public class CreateUserHandler : IRequestHandler<CreateUserCommand, string>
   {
       private readonly IUserRepository _userRepo;
       
       public CreateUserHandler(IUserRepository userRepo)
       {
           _userRepo = userRepo;
       }
       
       public async Task<string> Handle(CreateUserCommand request, CancellationToken cancellationToken)
       {
           var user = User.Create(request.Email, request.Password);
           await _userRepo.SaveAsync(user, cancellationToken);
           return user.Id;
       }
   }
   ```

### Infrastructure Layer

1. **Implement repositories**:

   ```csharp
   public class EfUserRepository : IUserRepository
   {
       private readonly ApplicationDbContext _context;
       
       public EfUserRepository(ApplicationDbContext context)
       {
           _context = context;
       }
       
       public async Task<User> GetByIdAsync(string id, CancellationToken cancellationToken)
       {
           return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
       }
       
       public async Task SaveAsync(User user, CancellationToken cancellationToken)
       {
           _context.Users.Add(user);
           await _context.SaveChangesAsync(cancellationToken);
       }
   }
   ```

2. **Use dependency injection**:

   ```csharp
   // In Program.cs or Startup.cs
   services.AddScoped<IUserRepository, EfUserRepository>();
   services.AddMediatR(typeof(CreateUserHandler));
   ```

3. **Repository best practices**:

   ```csharp
   // Define repository interfaces in the application layer
   public interface ITrainingRepository
   {
       Task<Training> GetByIdAsync(string id, CancellationToken cancellationToken);
       Task SaveAsync(Training training, CancellationToken cancellationToken);
       Task<IEnumerable<Training>> FindAllAvailableAsync(CancellationToken cancellationToken);
   }
   
   // Implement in the infrastructure layer
   public class EfTrainingRepository : ITrainingRepository
   {
       private readonly ApplicationDbContext _context;
       
       public EfTrainingRepository(ApplicationDbContext context)
       {
           _context = context;
       }
       
       // Implementation...
   }
   ```

## Entity Framework and Repository Pattern

### DbContext Design

```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        SavingChanges += OnSavingChanges;
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Training> Trainings { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Entity configurations
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.OwnsOne(e => e.Email, email =>
            {
                email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(256);
            });
        });
    }
    
    private void OnSavingChanges(object sender, SavingChangesEventArgs e)
    {
        // Update timestamps, sync tokens, etc.
        foreach (var entry in ChangeTracker.Entries<IHasMetadata>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedTime = DateTimeOffset.UtcNow;
                entry.Entity.UpdatedTime = DateTimeOffset.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedTime = DateTimeOffset.UtcNow;
                entry.Entity.SyncToken++;
            }
        }
    }
}
```

### Repository Implementation Best Practices

1. **Interface definition best practices**:

   ```csharp
   // Good: Domain-focused repository interface
   public interface IUserRepository
   {
       Task<User> GetByIdAsync(string id, CancellationToken cancellationToken);
       Task<User> FindByEmailAsync(string email, CancellationToken cancellationToken);
       Task SaveAsync(User user, CancellationToken cancellationToken);
       Task DeleteAsync(User user, CancellationToken cancellationToken);
   }
   
   // Bad: Database-focused repository interface
   public interface IUserRepository
   {
       Task<User> QueryAsync(string sql, object parameters);
       Task<int> ExecuteAsync(string sql, object parameters);
   }
   ```

2. **Transaction handling with Entity Framework**:

   ```csharp
   public class UserService
   {
       private readonly ApplicationDbContext _context;
       private readonly IUserRepository _userRepo;
       
       public UserService(ApplicationDbContext context, IUserRepository userRepo)
       {
           _context = context;
           _userRepo = userRepo;
       }
       
       public async Task TransferSubscriptionAsync(string fromId, string toId, CancellationToken cancellationToken)
       {
           using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
           try
           {
               var from = await _userRepo.GetByIdAsync(fromId, cancellationToken);
               var to = await _userRepo.GetByIdAsync(toId, cancellationToken);
               
               var subscription = from.RemoveSubscription();
               to.AddSubscription(subscription);
               
               await _userRepo.SaveAsync(from, cancellationToken);
               await _userRepo.SaveAsync(to, cancellationToken);
               
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

## Controller and API Design

### RESTful Controllers

```csharp
[Authorize]
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
    public async Task<ActionResult<string>> CreateUser([FromBody] CreateUserCommand command)
    {
        var userId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetUser), new { id = userId }, userId);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        var query = new GetUserByIdQuery { Id = id };
        var user = await _mediator.Send(query);
        return user ?? NotFound();
    }
}
```

## Testing Strategy

1. **Test pyramid approach**:
   - Unit tests for domain logic (fast, isolated)
   - Integration tests for repositories and controllers
   - Contract tests between services
   - Limited end-to-end tests for critical paths

2. **Domain testing**:

   ```csharp
   [Test]
   public void User_ChangePassword_WithValidCurrentPassword_ShouldUpdatePassword()
   {
       // Arrange
       var user = User.Create("test@example.com", "currentPassword");
       
       // Act
       user.ChangePassword("currentPassword", "newPassword");
       
       // Assert
       Assert.That(user.Password.Matches("newPassword"), Is.True);
   }
   ```

3. **Integration testing with Entity Framework**:

   ```csharp
   [Test]
   public async Task UserRepository_SaveAsync_ShouldPersistUser()
   {
       // Arrange
       using var context = CreateTestDbContext();
       var repository = new EfUserRepository(context);
       var user = User.Create("test@example.com", "password");
       
       // Act
       await repository.SaveAsync(user, CancellationToken.None);
       
       // Assert
       var savedUser = await repository.GetByIdAsync(user.Id, CancellationToken.None);
       Assert.That(savedUser, Is.Not.Null);
       Assert.That(savedUser.Email.Value, Is.EqualTo("test@example.com"));
   }
   ```

## Security Considerations

1. **Authorization in controllers**:

   ```csharp
   [Authorize(Policy = "CompanyAdmin")]
   [HttpPut("{id}")]
   public async Task<IActionResult> UpdateCompany(string id, [FromBody] UpdateCompanyCommand command)
   {
       command.Id = id;
       await _mediator.Send(command);
       return NoContent();
   }
   ```

2. **Input validation**:

   ```csharp
   public class CreateUserCommand : IRequest<string>
   {
       [Required]
       [EmailAddress]
       public string Email { get; set; }
       
       [Required]
       [MinLength(8)]
       public string Password { get; set; }
   }
   ```

3. **Repository security**:
   - Use parameterized queries (Entity Framework handles this automatically)
   - Implement proper authorization checks
   - Validate all inputs at the domain layer

## Configuration and Dependency Injection

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// Add MediatR
builder.Services.AddMediatR(typeof(CreateUserHandler));

// Add repositories
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<ITrainingRepository, EfTrainingRepository>();

// Add application services
builder.Services.AddScoped<UserService>();

var app = builder.Build();

// Configure middleware pipeline
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Conclusion

These guidelines provide a foundation for building maintainable .NET applications
using DDD, CQRS, and Clean Architecture principles. The goal is to create testable,
business-focused code that leverages .NET's strengths while maintaining architectural
clarity.

Remember that these patterns should serve your needs, not constrain you. Adapt
them as necessary for your specific context and requirements.
