# Software Engineering Principles for .NET Development

## Table of Contents

- [Software Engineering Principles for .NET Development](#software-engineering-principles-for-net-development)
  - [Table of Contents](#table-of-contents)
  - [1. DRY (Don't Repeat Yourself)](#1-dry-dont-repeat-yourself)
  - [2. YAGNI (You Aren't Gonna Need It)](#2-yagni-you-arent-gonna-need-it)
  - [3. KISS (Keep It Simple, Stupid)](#3-kiss-keep-it-simple-stupid)
  - [4. Single Responsibility Principle (SRP)](#4-single-responsibility-principle-srp)
  - [5. Readability Over Cleverness](#5-readability-over-cleverness)
  - [6. Avoid Premature Optimization](#6-avoid-premature-optimization)
  - [7. Test Early and Often](#7-test-early-and-often)
  - [8. Favor Composition Over Inheritance](#8-favor-composition-over-inheritance)
  - [9. Document Your Code](#9-document-your-code)
    - [Usage](#usage)
  - [10. Embrace Code Reviews](#10-embrace-code-reviews)
    - [Functionality](#functionality)
    - [Design](#design)
    - [Style and Readability](#style-and-readability)
    - [Performance](#performance)
    - [Security](#security)
    - [Testing](#testing)
  - [Additional .NET-Specific Principles](#additional-net-specific-principles)
    - [11. Use Async/Await Properly](#11-use-asyncawait-properly)
    - [12. Leverage Dependency Injection](#12-leverage-dependency-injection)
    - [13. Use Strong Typing](#13-use-strong-typing)
  - [Further Reading](#further-reading)

---

> _Adapted for .NET/C# development with practical examples and actionable advice_

This document summarizes key software engineering principles that are often overlooked,
with practical .NET/C# examples and Clean Architecture patterns.

---

## 1. DRY (Don't Repeat Yourself)

**Principle:**  
Avoid duplicating code or logic. Duplication increases maintenance cost and risk of inconsistencies.

**Bad Example:**

```csharp
public string GetUserName(User user)
{
    return $"{user.FirstName} {user.LastName}";
}

public string GetAuthorName(Author author)
{
    return $"{author.FirstName} {author.LastName}";
}
```

**Good Example:**

```csharp
public interface INameable
{
    string FirstName { get; }
    string LastName { get; }
}

public static class NameExtensions
{
    public static string GetFullName(this INameable entity)
    {
        return $"{entity.FirstName} {entity.LastName}";
    }
}

// Usage
var userName = user.GetFullName();
var authorName = author.GetFullName();
```

**Better Example with Generic Approach:**

```csharp
public abstract class PersonBase
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}

public class User : PersonBase
{
    public Guid Id { get; set; }
    public string Email { get; set; }
}

public class Author : PersonBase
{
    public Guid Id { get; set; }
    public List<Book> Books { get; set; } = new();
}
```

**Tip:**  
Extract common logic into base classes, extension methods, or services. Use inheritance judiciously and favor composition.

---

## 2. YAGNI (You Aren't Gonna Need It)

**Principle:**  
Don't implement features until they are actually needed. Premature generalization leads to wasted effort and complexity.

**Bad Example:**

```csharp
// Adding features "just in case"
public class Calculator
{
    public int Calculate(int a, int b, string operation)
    {
        return operation switch
        {
            "add" => a + b,
            "subtract" => a - b, // Not needed yet, but added anyway
            "multiply" => a * b, // Not needed yet, but added anyway
            "divide" => b != 0 ? a / b : 0, // Not needed yet, but added anyway
            _ => throw new ArgumentException("Invalid operation")
        };
    }
}
```

**Good Example:**

```csharp
// Only implement what's needed now
public static class MathOperations
{
    public static int Add(int a, int b) => a + b;
}

// Add more operations when actually needed
public static class MathOperations
{
    public static int Add(int a, int b) => a + b;
    public static int Subtract(int a, int b) => a - b; // Added when requirement arose
}
```

**Clean Architecture Example:**

```csharp
// Start with simple interface
public interface ICalculationService
{
    Task<int> AddAsync(int a, int b);
}

// Implement only what's needed
public class CalculationService : ICalculationService
{
    public Task<int> AddAsync(int a, int b)
    {
        return Task.FromResult(a + b);
    }
}
```

**Tip:**  
Focus on current requirements. Use interfaces to make future extensions easier when they're actually needed.

---

## 3. KISS (Keep It Simple, Stupid)

**Principle:**  
Prefer simple, clear solutions over clever or complex ones. Simplicity makes code easier to read, test, and maintain.

**Bad Example:**

```csharp
public bool IsEven(int number)
{
    if (number % 2 == 0)
    {
        return true;
    }
    else
    {
        return false;
    }
}

// Or overly clever
public bool IsEvenClever(int number) => (number & 1) == 0;
```

**Good Example:**

```csharp
public bool IsEven(int number) => number % 2 == 0;
```

**Complex vs Simple API Design:**

```csharp
// Complex - too many parameters
public class EmailService
{
    public async Task SendEmailAsync(string to, string from, string subject, 
        string body, bool isHtml, int priority, bool trackOpening, 
        string replyTo, List<string> cc, List<string> bcc)
    {
        // Implementation
    }
}

// Simple - use builder pattern or options
public class EmailService
{
    public async Task SendEmailAsync(EmailMessage message)
    {
        // Implementation
    }
}

public class EmailMessage
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public bool IsHtml { get; set; }
    // Optional properties with sensible defaults
    public string From { get; set; } = "noreply@company.com";
    public List<string> CC { get; set; } = new();
    public List<string> BCC { get; set; } = new();
}
```

**Tip:**  
If you can't explain your code simply, it's probably too complex. Use clear method names and avoid deep nesting.

---

## 4. Single Responsibility Principle (SRP)

**Principle:**  
A class should have only one reason to change. Each responsibility should be a separate concern.

**Bad Example:**

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }

    // Multiple responsibilities in one class
    public async Task SaveToDatabase()
    {
        // Database persistence logic
    }

    public bool IsValid()
    {
        // Validation logic
    }

    public async Task SendWelcomeEmail()
    {
        // Email sending logic
    }
}
```

**Good Example with Clean Architecture:**

```csharp
// Domain Entity - only domain logic
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string Name { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { } // EF Core constructor

    public User(string email, string name)
    {
        Id = Guid.NewGuid();
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        CreatedAt = DateTime.UtcNow;
        
        // Domain validation
        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));
    }

    private static bool IsValidEmail(string email)
    {
        return !string.IsNullOrEmpty(email) && email.Contains("@");
    }
}

// Repository - handles persistence
public interface IUserRepository
{
    Task<User> GetByIdAsync(Guid id);
    Task AddAsync(User user);
    Task<bool> ExistsAsync(string email);
}

// Service - orchestrates business operations
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public UserService(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public async Task<User> CreateUserAsync(string email, string name)
    {
        if (await _userRepository.ExistsAsync(email))
            throw new InvalidOperationException("User already exists");

        var user = new User(email, name);
        await _userRepository.AddAsync(user);
        await _emailService.SendWelcomeEmailAsync(email);
        
        return user;
    }
}

// Email Service - handles email operations
public interface IEmailService
{
    Task SendWelcomeEmailAsync(string email);
}
```

**Tip:**  
Use dependency injection to separate concerns. Domain entities should contain only domain logic, not infrastructure concerns.

---

## 5. Readability Over Cleverness

**Principle:**  
Write code for humans first, computers second. Favor clarity over brevity or clever tricks.

**Bad Example:**

```csharp
// Overly condensed and clever
var result = users.Where(u => u.IsActive && u.LastLogin > DateTime.Now.AddDays(-30))
    .SelectMany(u => u.Orders.Where(o => o.Status == "Completed" && o.Total > 100))
    .GroupBy(o => o.UserId).Select(g => new { UserId = g.Key, Total = g.Sum(o => o.Total) })
    .OrderByDescending(x => x.Total).Take(10).ToList();
```

**Good Example:**

```csharp
// Clear and readable
var activeUsers = users.Where(u => u.IsActive && u.LastLogin > DateTime.Now.AddDays(-30));

var highValueOrders = activeUsers
    .SelectMany(user => user.Orders)
    .Where(order => order.Status == "Completed" && order.Total > 100);

var userOrderTotals = highValueOrders
    .GroupBy(order => order.UserId)
    .Select(group => new UserOrderSummary
    {
        UserId = group.Key,
        TotalOrderValue = group.Sum(order => order.Total)
    });

var topCustomers = userOrderTotals
    .OrderByDescending(summary => summary.TotalOrderValue)
    .Take(10)
    .ToList();
```

**Better with Explicit Method:**

```csharp
public class CustomerAnalyticsService
{
    public async Task<List<UserOrderSummary>> GetTopCustomersAsync(int count = 10)
    {
        var recentlyActiveUsers = await GetRecentlyActiveUsersAsync();
        var highValueOrders = await GetHighValueOrdersAsync(recentlyActiveUsers);
        
        return highValueOrders
            .GroupBy(order => order.UserId)
            .Select(CreateUserOrderSummary)
            .OrderByDescending(summary => summary.TotalOrderValue)
            .Take(count)
            .ToList();
    }

    private async Task<List<User>> GetRecentlyActiveUsersAsync()
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        return await _userRepository.GetActiveUsersSinceAsync(thirtyDaysAgo);
    }

    private static UserOrderSummary CreateUserOrderSummary(IGrouping<Guid, Order> orderGroup)
    {
        return new UserOrderSummary
        {
            UserId = orderGroup.Key,
            TotalOrderValue = orderGroup.Sum(order => order.Total),
            OrderCount = orderGroup.Count()
        };
    }
}
```

**Tip:**  
Use meaningful variable names, extract methods for complex logic, and prefer explicit over implicit operations.

---

## 6. Avoid Premature Optimization

**Principle:**  
First make it work, then make it right, then make it fast. Optimize only when necessary and with evidence.

**Example - Don't Optimize Without Measuring:**

```csharp
// Don't start with complex caching without measuring need
public class ProductService
{
    private readonly IProductRepository _repository;

    // Start simple
    public async Task<Product> GetProductAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }
}

// Add caching only when performance testing shows it's needed
public class ProductService
{
    private readonly IProductRepository _repository;
    private readonly IMemoryCache _cache;

    public async Task<Product> GetProductAsync(Guid id)
    {
        var cacheKey = $"product_{id}";
        
        if (_cache.TryGetValue(cacheKey, out Product cachedProduct))
            return cachedProduct;

        var product = await _repository.GetByIdAsync(id);
        
        _cache.Set(cacheKey, product, TimeSpan.FromMinutes(5));
        return product;
    }
}
```

**Tip:**  
Use profiling tools like BenchmarkDotNet, Application Insights, or Visual Studio Diagnostic Tools before optimizing.

---

## 7. Test Early and Often

**Principle:**  
Write tests as you develop. Tests help catch bugs early and make refactoring safer.

**Example with xUnit and Clean Architecture:**

```csharp
// Domain Logic Test
public class UserTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var email = "test@example.com";
        var name = "Test User";

        // Act
        var user = new User(email, name);

        // Assert
        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
        user.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData(null)]
    public void Constructor_WithInvalidEmail_ShouldThrowException(string invalidEmail)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new User(invalidEmail, "Test User"));
    }
}

// Service Test with Mocking
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _userService = new UserService(_userRepositoryMock.Object, _emailServiceMock.Object);
    }

    [Fact]
    public async Task CreateUserAsync_WithValidData_ShouldCreateUserAndSendEmail()
    {
        // Arrange
        var email = "test@example.com";
        var name = "Test User";

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(email))
            .ReturnsAsync(false);

        // Act
        var user = await _userService.CreateUserAsync(email, name);

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be(email);
        
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendWelcomeEmailAsync(email), Times.Once);
    }
}
```

**Tip:**  
Aim for high test coverage, use meaningful test names, and follow the AAA pattern (Arrange, Act, Assert).

---

## 8. Favor Composition Over Inheritance

**Principle:**  
Compose behavior using interfaces and dependency injection rather than deep inheritance trees.

**Bad Example:**

```csharp
public class Animal
{
    public virtual void Move() => Console.WriteLine("Moving");
}

public class Mammal : Animal
{
    public virtual void Breathe() => Console.WriteLine("Breathing");
}

public class Dog : Mammal
{
    public override void Move() => Console.WriteLine("Running");
    public void Bark() => Console.WriteLine("Barking");
}

// Deep inheritance chain becomes hard to maintain
```

**Good Example with Composition:**

```csharp
public interface IMovable
{
    void Move();
}

public interface ISoundMaker
{
    void MakeSound();
}

public interface IBreather
{
    void Breathe();
}

public class Dog : IMovable, ISoundMaker, IBreather
{
    private readonly IMovementBehavior _movementBehavior;
    private readonly ISoundBehavior _soundBehavior;

    public Dog(IMovementBehavior movementBehavior, ISoundBehavior soundBehavior)
    {
        _movementBehavior = movementBehavior;
        _soundBehavior = soundBehavior;
    }

    public void Move() => _movementBehavior.Move();
    public void MakeSound() => _soundBehavior.MakeSound();
    public void Breathe() => Console.WriteLine("Breathing");
}

public interface IMovementBehavior
{
    void Move();
}

public class RunningBehavior : IMovementBehavior
{
    public void Move() => Console.WriteLine("Running");
}

public interface ISoundBehavior
{
    void MakeSound();
}

public class BarkingBehavior : ISoundBehavior
{
    public void MakeSound() => Console.WriteLine("Barking");
}
```

**Clean Architecture Example:**

```csharp
// Instead of inheritance, use services and interfaces
public interface INotificationService
{
    Task SendAsync(string message, string recipient);
}

public interface IEmailNotificationService : INotificationService { }
public interface ISmsNotificationService : INotificationService { }

public class CompositeNotificationService : INotificationService
{
    private readonly IEnumerable<INotificationService> _notificationServices;

    public CompositeNotificationService(IEnumerable<INotificationService> notificationServices)
    {
        _notificationServices = notificationServices;
    }

    public async Task SendAsync(string message, string recipient)
    {
        var tasks = _notificationServices.Select(service => service.SendAsync(message, recipient));
        await Task.WhenAll(tasks);
    }
}
```

**Tip:**  
Use dependency injection to compose behavior. Prefer interfaces over abstract classes for contracts.

---

## 9. Document Your Code

**Principle:**  
Good documentation helps others (and your future self) understand and use your code.

**Example with XML Documentation:**

```csharp
/// <summary>
/// Calculates the total price including tax for a given amount.
/// </summary>
/// <param name="amount">The base amount before tax</param>
/// <param name="taxRate">The tax rate as a decimal (e.g., 0.1 for 10%)</param>
/// <returns>The total amount including tax</returns>
/// <exception cref="ArgumentException">Thrown when amount is negative or taxRate is invalid</exception>
/// <example>
/// <code>
/// var total = CalculateTotalWithTax(100, 0.1); // Returns 110
/// </code>
/// </example>
public static decimal CalculateTotalWithTax(decimal amount, decimal taxRate)
{
    if (amount < 0)
        throw new ArgumentException("Amount cannot be negative", nameof(amount));
    
    if (taxRate < 0 || taxRate > 1)
        throw new ArgumentException("Tax rate must be between 0 and 1", nameof(taxRate));

    return amount * (1 + taxRate);
}

/// <summary>
/// Repository for managing user entities in the database.
/// </summary>
/// <remarks>
/// This repository implements the Repository pattern and provides
/// async methods for CRUD operations on User entities.
/// </remarks>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByIdAsync(Guid id);

    /// <summary>
    /// Adds a new user to the repository.
    /// </summary>
    /// <param name="user">The user to add</param>
    /// <exception cref="ArgumentNullException">Thrown when user is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when user already exists</exception>
    Task AddAsync(User user);
}
```

**README Documentation Example:**

```markdown
# UserManagement Service

## Overview
This service handles user registration, authentication, and profile management.

## Features
- User registration with email verification
- JWT-based authentication
- Profile management
- Password reset functionality

## Getting Started

### Prerequisites
- .NET 9.0 or later
- SQL Server or PostgreSQL
- Valkey/Redis (for caching)

### Configuration
Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-connection-string"
  },
  "Jwt": {
    "SecretKey": "your-secret-key",
    "ExpiryMinutes": 60
  }
}
```

### Usage

```csharp
// Register a new user
var user = await userService.CreateUserAsync("test@example.com", "password");

// Authenticate
var token = await authService.AuthenticateAsync("test@example.com", "password");
```

**Tip:**  
Use XML documentation for public APIs, maintain updated README files, and document complex business logic.

---

## 10. Embrace Code Reviews

**Principle:**  
Peer reviews catch issues you might miss and improve code quality.

**Code Review Checklist:**

### Functionality

- [ ] Does the code do what it's supposed to do?
- [ ] Are edge cases handled properly?
- [ ] Is error handling appropriate?

### Design

- [ ] Does the code follow SOLID principles?
- [ ] Are design patterns used appropriately?
- [ ] Is the code following Clean Architecture principles?

### Style and Readability

- [ ] Is the code readable and self-documenting?
- [ ] Are naming conventions followed?
- [ ] Is the code properly formatted?

### Performance

- [ ] Are there any obvious performance issues?
- [ ] Is database access optimized?
- [ ] Are async/await patterns used correctly?

### Security

- [ ] Are inputs properly validated?
- [ ] Is sensitive data handled securely?
- [ ] Are authorization checks in place?

### Testing

- [ ] Are there sufficient unit tests?
- [ ] Do integration tests cover the main scenarios?
- [ ] Is test coverage adequate?

**Example Review Comments:**

```csharp
// ❌ Poor - No context
// Fix this

// ✅ Good - Specific and educational
// Consider extracting this logic into a separate method for better testability
// and to follow the Single Responsibility Principle

// ❌ Poor - Too harsh
// This is wrong

// ✅ Good - Constructive
// This approach might lead to performance issues with large datasets.
// Consider using pagination or implementing a more efficient query strategy.
```

**Tip:**  
Be constructive in reviews, ask questions to understand intent, and suggest improvements rather than just pointing out problems.

---

## Additional .NET-Specific Principles

### 11. Use Async/Await Properly

**Bad Example:**

```csharp
// Blocking async calls
public User GetUser(Guid id)
{
    return _userRepository.GetByIdAsync(id).Result; // Deadlock risk!
}

// Async all the way down
public async Task<User> GetUserAsync(Guid id)
{
    var user = await _userRepository.GetByIdAsync(id);
    
    // Don't await unnecessarily
    await Task.Delay(100); // Why?
    
    return user;
}
```

**Good Example:**

```csharp
public async Task<User> GetUserAsync(Guid id)
{
    return await _userRepository.GetByIdAsync(id);
}

// Configure await when you don't need context
public async Task<User> GetUserAsync(Guid id)
{
    return await _userRepository.GetByIdAsync(id).ConfigureAwait(false);
}
```

### 12. Leverage Dependency Injection

**Bad Example:**

```csharp
public class UserController : ControllerBase
{
    public async Task<IActionResult> GetUser(Guid id)
    {
        // Tight coupling
        var repository = new UserRepository();
        var user = await repository.GetByIdAsync(id);
        return Ok(user);
    }
}
```

**Good Example:**

```csharp
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userService.GetUserAsync(id);
        return Ok(user);
    }
}
```

### 13. Use Strong Typing

**Bad Example:**

```csharp
public async Task ProcessOrder(string orderId, string customerId, decimal amount)
{
    // Risk of parameter confusion
}
```

**Good Example:**

```csharp
public record OrderId(Guid Value);
public record CustomerId(Guid Value);
public record Amount(decimal Value);

public async Task ProcessOrder(OrderId orderId, CustomerId customerId, Amount amount)
{
    // Type safety prevents parameter confusion
}
```

## Further Reading

- [Clean Code by Robert C. Martin](https://www.amazon.com/Clean-Code-Handbook-Software-Craftsmanship/dp/0132350884)
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Clean Architecture with .NET](https://docs.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [Dependency Injection in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [ASP.NET Core Best Practices](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
