# .NET Testing Best Practices

## Table of Contents

- [Core Testing Philosophy](#core-testing-philosophy)
- [Project-Specific Testing Structure](#project-specific-testing-structure)
- [Test Organization](#test-organization)
- [Mocking and Dependency Injection](#mocking-and-dependency-injection)
- [Testing Async Code](#testing-async-code)
- [Testing Entity Framework and Database Operations](#testing-entity-framework-and-database-operations)
- [Testing Web APIs](#testing-web-apis)
- [Testing MediatR Commands and Queries](#testing-mediatr-commands-and-queries)
- [Recommended Testing Libraries](#recommended-testing-libraries)
- [Common Testing Anti-patterns to Avoid](#common-testing-anti-patterns-to-avoid)
- [Test Coverage and Quality](#test-coverage-and-quality)
- [Architecture Testing](#architecture-testing)
- [Performance Testing](#performance-testing)
- [Further Reading](#further-reading)

---

## Core Testing Philosophy

- **Test behavior, not implementation**: Focus on _what_ the code does, not _how_ it does it
- **Aim for high coverage, but prioritize critical paths**
- **Integration tests complement unit tests** but should be kept separate
- **Test edge cases and error conditions thoroughly**
- **Keep tests readable and maintainable**: Use descriptive names and clear structure
- **Prefer data-driven tests** for clarity and coverage
- **Follow the AAA pattern**: Arrange, Act, Assert

<!-- REF: https://docs.microsoft.com/en-us/dotnet/core/testing/ -->
<!-- REF: https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests -->

## Project-Specific Testing Structure

```bash
# Recommended .NET testing structure
ProjectName/
├── src/
│   ├── ProjectName.Domain/
│   ├── ProjectName.Application/
│   ├── ProjectName.Infrastructure/
│   └── ProjectName.WebApi/
└── tests/
    ├── ProjectName.UnitTests/
    ├── ProjectName.IntegrationTests/
    └── ProjectName.ArchitectureTests/
```

## Test Organization

### Unit Test Structure with xUnit

```csharp
public class UserServiceTests
{
    [Fact]
    public void CreateUser_WithValidData_ShouldReturnUser()
    {
        // ARRANGE: Set up test data and expectations
        var userService = new UserService();
        var createUserRequest = new CreateUserRequest 
        { 
            Email = "test@example.com", 
            Name = "Test User" 
        };

        // ACT: Call the method being tested
        var result = userService.CreateUser(createUserRequest);

        // ASSERT: Verify the results
        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
        result.Name.Should().Be("Test User");
    }
}
```

### Data-Driven Tests with xUnit Theory

Preferred for methods with multiple input/output scenarios:

```csharp
public class EmailValidationTests
{
    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("userexample.com", false)]
    [InlineData("user@", false)]
    [InlineData("user@example.123", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidEmail_ShouldReturnExpectedResult(string email, bool expected)
    {
        // ACT
        var result = EmailValidator.IsValid(email);

        // ASSERT
        result.Should().Be(expected);
    }

    // Complex test data using MemberData
    [Theory]
    [MemberData(nameof(GetEmailTestData))]
    public void ValidateEmail_WithComplexScenarios_ShouldReturnExpectedResult(
        string email, bool isValid, string expectedMessage)
    {
        // ACT
        var result = EmailValidator.Validate(email);

        // ASSERT
        result.IsValid.Should().Be(isValid);
        result.Message.Should().Be(expectedMessage);
    }

    public static IEnumerable<object[]> GetEmailTestData()
    {
        yield return new object[] { "valid@example.com", true, "Email is valid" };
        yield return new object[] { "invalid", false, "Email format is invalid" };
        yield return new object[] { "", false, "Email is required" };
    }
}
```

### Testing with Fixtures and Shared Context

```csharp
// Shared test fixture
public class DatabaseFixture : IDisposable
{
    public ApplicationDbContext Context { get; private set; }

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}

// Test class using fixture
public class UserRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUserById_WithExistingUser_ShouldReturnUser()
    {
        // ARRANGE
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com" };
        _fixture.Context.Users.Add(user);
        await _fixture.Context.SaveChangesAsync();

        var repository = new UserRepository(_fixture.Context);

        // ACT
        var result = await repository.GetByIdAsync(user.Id);

        // ASSERT
        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
    }
}
```

### Parallel Test Execution

```csharp
// xUnit runs tests in parallel by default within a class
// Use Collection attribute to control parallelism
[Collection("Database collection")]
public class UserServiceTests
{
    // Tests that share database state
}

[Collection("Database collection")]
public class OrderServiceTests
{
    // Tests that share database state
}

// Define collection to prevent parallel execution
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // This class has no code, and is never instantiated.
    // Its purpose is simply to be the place to apply [CollectionDefinition]
}
```

## Mocking and Dependency Injection

### Using Moq for Mocking

```csharp
public class OrderServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _orderService = new OrderService(_userRepositoryMock.Object, _emailServiceMock.Object);
    }

    [Fact]
    public async Task CreateOrder_WithValidUser_ShouldSendConfirmationEmail()
    {
        // ARRANGE
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com" };
        var orderRequest = new CreateOrderRequest { UserId = userId, ProductId = Guid.NewGuid() };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _emailServiceMock
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _orderService.CreateOrderAsync(orderRequest);

        // ASSERT
        result.Should().NotBeNull();
        _emailServiceMock.Verify(
            x => x.SendEmailAsync(user.Email, "Order Confirmation", It.IsAny<string>()),
            Times.Once);
    }
}
```

### Using NSubstitute (Alternative to Moq)

```csharp
public class ProductServiceTests
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _productRepository = Substitute.For<IProductRepository>();
        _logger = Substitute.For<ILogger<ProductService>>();
        _productService = new ProductService(_productRepository, _logger);
    }

    [Fact]
    public async Task GetProduct_WhenNotFound_ShouldThrowNotFoundException()
    {
        // ARRANGE
        var productId = Guid.NewGuid();
        _productRepository.GetByIdAsync(productId).Returns((Product)null);

        // ACT & ASSERT
        await Assert.ThrowsAsync<NotFoundException>(
            () => _productService.GetProductAsync(productId));

        _logger.Received(1).LogWarning(Arg.Any<string>(), productId);
    }
}
```

## Testing Async Code

### Testing Async Methods

```csharp
[Fact]
public async Task ProcessDataAsync_WithValidData_ShouldCompleteSuccessfully()
{
    // ARRANGE
    var data = new[] { "item1", "item2", "item3" };
    var processor = new DataProcessor();

    // ACT
    var result = await processor.ProcessDataAsync(data);

    // ASSERT
    result.Should().NotBeNull();
    result.ProcessedCount.Should().Be(3);
}

[Fact]
public async Task ProcessDataAsync_WithCancellation_ShouldThrowOperationCanceledException()
{
    // ARRANGE
    var data = new[] { "item1", "item2", "item3" };
    var processor = new DataProcessor();
    var cts = new CancellationTokenSource();
    cts.Cancel(); // Cancel immediately

    // ACT & ASSERT
    await Assert.ThrowsAsync<OperationCanceledException>(
        () => processor.ProcessDataAsync(data, cts.Token));
}
```

### Testing Concurrent Code

```csharp
[Fact]
public async Task ProcessConcurrentRequests_ShouldHandleMultipleRequestsSafely()
{
    // ARRANGE
    var service = new ThreadSafeService();
    var tasks = new List<Task<int>>();

    // ACT: Create multiple concurrent requests
    for (int i = 0; i < 10; i++)
    {
        int localI = i; // Capture loop variable
        tasks.Add(Task.Run(() => service.ProcessRequest(localI)));
    }

    var results = await Task.WhenAll(tasks);

    // ASSERT
    results.Should().HaveCount(10);
    results.Should().OnlyHaveUniqueItems();
}

[Fact]
public async Task BackgroundService_ShouldProcessItemsWithinTimeout()
{
    // ARRANGE
    var service = new BackgroundProcessingService();
    var completionSource = new TaskCompletionSource<bool>();

    service.OnProcessingComplete += () => completionSource.SetResult(true);

    // ACT
    service.Start();

    // ASSERT: Wait for completion with timeout
    var completed = await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(5));
    completed.Should().BeTrue();
}
```

## Testing Entity Framework and Database Operations

### Using In-Memory Database

```csharp
public class UserRepositoryTests
{
    private ApplicationDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task AddUser_ShouldPersistUser()
    {
        // ARRANGE
        using var context = GetInMemoryContext();
        var repository = new UserRepository(context);
        var user = new User { Email = "test@example.com", Name = "Test User" };

        // ACT
        await repository.AddAsync(user);
        await context.SaveChangesAsync();

        // ASSERT
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        savedUser.Should().NotBeNull();
        savedUser.Name.Should().Be("Test User");
    }
}
```

### Using TestContainers for Real Database Testing

```csharp
public class DatabaseIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:13")
        .WithDatabase("testdb")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task DatabaseOperations_WithRealDatabase_ShouldWork()
    {
        // ARRANGE
        var connectionString = _postgres.GetConnectionString();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var repository = new UserRepository(context);
        var user = new User { Email = "test@example.com", Name = "Test User" };

        // ACT
        await repository.AddAsync(user);
        await context.SaveChangesAsync();

        // ASSERT
        var savedUser = await repository.GetByEmailAsync("test@example.com");
        savedUser.Should().NotBeNull();
        savedUser.Name.Should().Be("Test User");
    }
}
```

## Testing Web APIs

### Integration Testing with WebApplicationFactory

```csharp
public class UsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UsersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ShouldReturnOkWithUsers()
    {
        // ACT
        var response = await _client.GetAsync("/api/users");

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserDto>>(content);
        users.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateUser_WithValidData_ShouldReturnCreated()
    {
        // ARRANGE
        var createRequest = new CreateUserRequest
        {
            Email = "newuser@example.com",
            Name = "New User"
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // ACT
        var response = await _client.PostAsync("/api/users", content);

        // ASSERT
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var createdUser = JsonSerializer.Deserialize<UserDto>(responseContent);
        createdUser.Email.Should().Be("newuser@example.com");
    }
}
```

### Custom WebApplicationFactory for Testing

```csharp
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> 
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace real database with in-memory for testing
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Replace real email service with mock
            services.Replace(ServiceDescriptor.Scoped<IEmailService, MockEmailService>());
        });

        builder.UseEnvironment("Testing");
    }
}
```

## Testing MediatR Commands and Queries

```csharp
public class CreateUserCommandTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _handler = new CreateUserCommandHandler(_userRepositoryMock.Object, _emailServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateUserAndSendWelcomeEmail()
    {
        // ARRANGE
        var command = new CreateUserCommand("test@example.com", "Test User");

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(command.Email))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        // ACT
        var result = await _handler.Handle(command, CancellationToken.None);

        // ASSERT
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u => 
            u.Email == command.Email && u.Name == command.Name)), Times.Once);

        _emailServiceMock.Verify(x => x.SendWelcomeEmailAsync(command.Email), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ShouldReturnFailure()
    {
        // ARRANGE
        var command = new CreateUserCommand("existing@example.com", "Test User");

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(command.Email))
            .ReturnsAsync(true);

        // ACT
        var result = await _handler.Handle(command, CancellationToken.None);

        // ASSERT
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User with this email already exists");

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        _emailServiceMock.Verify(x => x.SendWelcomeEmailAsync(It.IsAny<string>()), Times.Never);
    }
}
```

## Recommended Testing Libraries

### Core Testing Framework

- **xUnit**: Preferred testing framework for .NET
- **NUnit**: Alternative testing framework
- **MSTest**: Microsoft's testing framework

### Assertion Libraries

- **FluentAssertions**: More readable assertions
- **Shouldly**: Alternative assertion library

### Mocking

- **Moq**: Most popular mocking framework
- **NSubstitute**: Alternative with cleaner syntax
- **FakeItEasy**: Another mocking alternative

### Integration Testing

- **TestContainers**: Real database/service testing
- **WebApplicationFactory**: ASP.NET Core integration testing
- **WireMock.Net**: HTTP service mocking

### Specialized Testing

- **Bogus**: Test data generation
- **AutoFixture**: Automated test data creation
- **NetArchTest**: Architecture testing
- **ArchUnitNET**: Architecture validation

```csharp
// Example using Bogus for test data generation
public class UserTestDataBuilder
{
    private readonly Faker<User> _userFaker;

    public UserTestDataBuilder()
    {
        _userFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.CreatedAt, f => f.Date.Recent());
    }

    public User Build() => _userFaker.Generate();
    public List<User> BuildMany(int count) => _userFaker.Generate(count);
}
```

## Common Testing Anti-patterns to Avoid

1. **Testing implementation details** rather than behavior
2. **Brittle tests** that break when internal implementation changes
3. **Slow tests** that access real resources unnecessarily
4. **Global state** that makes tests non-deterministic
5. **Partial assertions** that don't verify complete behavior
6. **Magic numbers and strings** without explanation
7. **Not testing error scenarios** and edge cases
8. **Tests that depend on external services** without proper isolation
9. **Overly complex test setup** that obscures the test intent
10. **Not using proper async/await** patterns in async tests

## Test Coverage and Quality

### Using Coverlet for Code Coverage

```xml
<!-- In test project file -->
<PackageReference Include="coverlet.collector" Version="3.1.2">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report (requires ReportGenerator)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html

# View coverage report
open coveragereport/index.html
```

### Quality Gates

Aim for high coverage of critical components:

- Domain logic: >90%
- Application services: >85%
- Controllers: >80%
- Infrastructure: >70%

## Architecture Testing

```csharp
public class ArchitectureTests
{
    [Fact]
    public void Domain_ShouldNotDependOnInfrastructure()
    {
        var domainAssembly = typeof(User).Assembly;
        var infrastructureAssembly = typeof(ApplicationDbContext).Assembly;

        var result = Types.InAssembly(domainAssembly)
            .Should()
            .NotHaveDependencyOn(infrastructureAssembly.GetName().Name);

        result.Should().BeSuccessful();
    }

    [Fact]
    public void Controllers_ShouldHaveControllerSuffix()
    {
        var result = Types.InCurrentDomain()
            .That()
            .ResideInNamespace("ProjectName.WebApi.Controllers")
            .Should()
            .HaveNameEndingWith("Controller");

        result.Should().BeSuccessful();
    }
}
```

## Performance Testing

```csharp
[Fact]
public async Task GetUsers_ShouldCompleteWithinReasonableTime()
{
    // ARRANGE
    var stopwatch = Stopwatch.StartNew();
    var service = new UserService();

    // ACT
    var result = await service.GetUsersAsync();

    // ASSERT
    stopwatch.Stop();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
    result.Should().NotBeNull();
}

[Fact]
public async Task ProcessLargeDataSet_ShouldHandleMemoryEfficiently()
{
    // ARRANGE
    var initialMemory = GC.GetTotalMemory(true);
    var service = new DataProcessingService();
    var largeDataSet = Enumerable.Range(1, 100000).ToArray();

    // ACT
    await service.ProcessDataAsync(largeDataSet);

    // ASSERT
    GC.Collect();
    var finalMemory = GC.GetTotalMemory(true);
    var memoryIncrease = finalMemory - initialMemory;
    
    memoryIncrease.Should().BeLessThan(50 * 1024 * 1024); // Less than 50MB increase
}
```

## Further Reading

- [.NET Testing Documentation](https://docs.microsoft.com/en-us/dotnet/core/testing/)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [ASP.NET Core Integration Testing](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [TestContainers for .NET](https://dotnet.testcontainers.org/)
- [Clean Architecture Testing Strategies](https://blog.cleancoder.com/uncle-bob/2017/10/03/TestContravariance.html)
