# Security-First Repository Design in .NET 9

This document provides comprehensive guidance for implementing security-first
repository patterns in .NET 9 applications, following Clean Architecture and DDD
principles. It focuses on creating repositories that are secure by design, making
it impossible to use them incorrectly.

## Table of Contents

- [Core Security Principles](#core-security-principles)
- [Domain-Driven Security](#domain-driven-security)
- [Implementation Patterns](#implementation-patterns)
- [Collection Security](#collection-security)
- [Internal Operations](#internal-operations)
- [Anti-Patterns to Avoid](#anti-patterns-to-avoid)
- [Testing Security](#testing-security)
- [Advanced Patterns](#advanced-patterns)

## Core Security Principles

### Security by Design

The fundamental principle of secure repository design is to make it
**impossible to use the repository incorrectly**. Rather than relying on developers
to remember security checks, we build security directly into the repository interface.

**Key Concepts:**

- **Fail Secure**: Default to denying access rather than allowing it
- **Explicit Authorization**: Make authorization requirements visible in method signatures
- **Domain-Driven Security**: Security rules should reflect business domain concepts
- **Static Type Safety**: Use .NET's type system to enforce security at compile time

### Authorization at the Repository Level

While some may debate whether authorization belongs in the repository, for business
applications it provides significant advantages:

- **Single Point of Control**: All data access goes through secured repositories
- **Cannot Be Bypassed**: No way to accidentally skip authorization checks
- **Domain Alignment**: Security rules match business concepts
- **Testable**: Easy to unit test authorization logic

## Domain-Driven Security

### Security as Domain Logic

Security rules are often core business logic that should be modeled in the domain
layer. For example, "only resource owners and administrators can see resource
details" is a business rule, not a technical detail.

```csharp
namespace YourProject.Domain.Security
{
    // User represents the authenticated user context
    public class UserContext
    {
        public required string UserId { get; init; }
        public required List<string> Roles { get; init; }
        public required List<string> Permissions { get; init; }

        public bool HasRole(string role) => Roles.Contains(role);
        public bool HasPermission(string permission) => Permissions.Contains(permission);
        public bool IsOwnerOf(string resourceOwnerId) => UserId == resourceOwnerId;
    }

    // Resource represents a business entity with ownership
    public class Resource : AggregateRoot
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string OwnerId { get; private set; }
        public DateTime CreatedAt { get; private set; }

        // Business rule: Who can view this resource?
        public bool CanBeViewedBy(UserContext user)
        {
            return user.IsOwnerOf(OwnerId) ||
                   user.HasRole("Admin") ||
                   user.HasPermission("resource:view_all");
        }

        // Business rule: Who can modify this resource?
        public bool CanBeModifiedBy(UserContext user)
        {
            return user.IsOwnerOf(OwnerId) ||
                   user.HasRole("Admin") ||
                   user.HasPermission("resource:manage_all");
        }
    }
}

```csharp
namespace Jacopo.Domain.Security
{
    // User represents the authenticated user context
    public class UserContext
    {
        public required string UserId { get; init; }
        public required UserRole Role { get; init; }
        public required string TenantId { get; init; }
        public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();

        public bool HasPermission(string permission) =>
            Permissions.Contains(permission) || Role == UserRole.SystemAdmin;

        public bool CanAccessTenant(string tenantId) =>
            Role == UserRole.SystemAdmin || TenantId == tenantId;
    }

    public enum UserRole
    {
        User,
        TenantAdmin,
        SystemAdmin
    }

    // Training represents a business entity with ownership
    public class Training : AggregateRoot
    {
        public Guid Id { get; private set; }
        public required string Name { get; init; }
        public required string OwnerId { get; init; }
        public required string TenantId { get; init; }
        public IReadOnlyList<string> TrainerIds { get; private set; } = Array.Empty<string>();

        // Domain security rules
        public bool CanBeViewedBy(UserContext user)
        {
            return user.Role == UserRole.SystemAdmin ||
                   user.CanAccessTenant(TenantId) && (
                       OwnerId == user.UserId ||
                       TrainerIds.Contains(user.UserId) ||
                       user.HasPermission("training:view_all")
                   );
        }

        public bool CanBeModifiedBy(UserContext user)
        {
            return user.Role == UserRole.SystemAdmin ||
                   user.CanAccessTenant(TenantId) && (
                       OwnerId == user.UserId ||
                       user.HasPermission("training:manage_all")
                   );
        }
    }
}
```

### Security Error Types

Security violations should be represented as domain-specific exceptions that provide clear information about what went wrong:

```csharp
namespace YourProject.Domain.Security.Exceptions
{
    public class SecurityViolationException : DomainException
    {
        public string RequestingUserId { get; }

        protected SecurityViolationException(string message, string requestingUserId) : base(message)
        {
            RequestingUserId = requestingUserId;
        }
    }

    public class ForbiddenToViewResourceException : SecurityViolationException
    {
        public string ResourceOwnerId { get; }

        public ForbiddenToViewResourceException(string requestingUserId, string resourceOwnerId)
            : base($"User '{requestingUserId}' cannot access resource owned by '{resourceOwnerId}'")
        {
            ResourceOwnerId = resourceOwnerId;
        }
    }

    public class ForbiddenToModifyResourceException : SecurityViolationException
    {
        public Guid ResourceId { get; }

        public ForbiddenToModifyResourceException(string requestingUserId, Guid resourceId)
            : base($"User '{requestingUserId}' cannot modify resource '{resourceId}'")
        {
            ResourceId = resourceId;
        }
    }
```

### Domain Security Rules

Business security rules should be clearly defined in the domain layer:

```csharp
namespace YourProject.Domain.Security
{
    public static class ResourceSecurityRules
    {
        public static void CanUserViewResource(UserContext user, Resource resource)
        {
            if (!resource.CanBeViewedBy(user))
            {
                throw new ForbiddenToViewResourceException(user.UserId, resource.OwnerId);
            }
        }

        public static void CanUserModifyResource(UserContext user, Resource resource)
        {
            if (!resource.CanBeModifiedBy(user))
            {
                throw new ForbiddenToModifyResourceException(user.UserId, resource.Id);
            }
        }
    }
}
```

### Modeling User Context

Create a domain `UserContext` type that encapsulates authentication context rather than using primitive types:

```csharp
// Good: Type-safe user context with strong typing
public class UserContext
{
    public required string UserId { get; init; }
    public required UserRole Role { get; init; }
    public required string TenantId { get; init; }
    public IReadOnlyList<Permission> Permissions { get; init; } = Array.Empty<Permission>();

    public bool HasPermission(Permission permission) =>
        Permissions.Contains(permission) || Role == UserRole.SystemAdmin;
}

public record Permission(string Value)
{
    public static Permission ViewAllTrainings => new("training:view_all");
    public static Permission ManageAllTrainings => new("training:manage_all");
    public static Permission AdminAccess => new("admin:access");
}

// Avoid: Primitive obsession
Task<Training?> GetTrainingAsync(Guid trainingId, string userId);

// Better: Domain-driven user context
Task<Training?> GetTrainingAsync(Guid trainingId, UserContext user);
```

## Implementation Patterns

### Secure Repository Interface

Design repository interfaces that require user context for all operations that need authorization:

```csharp
namespace YourProject.Domain.Resources
{
    // ResourceRepository defines the interface in the domain layer
    public interface IResourceRepository
    {
        // Operations requiring authorization
        Task<Resource?> GetResourceAsync(Guid resourceId, UserContext user, CancellationToken cancellationToken = default);
        Task UpdateResourceAsync(Guid resourceId, UserContext user, Func<Resource, Resource> updateFn, CancellationToken cancellationToken = default);
        Task DeleteResourceAsync(Guid resourceId, UserContext user, CancellationToken cancellationToken = default);

        // Collection operations with built-in filtering
        Task<IReadOnlyList<Resource>> FindResourcesForUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Resource>> FindResourcesForAdminAsync(string adminId, CancellationToken cancellationToken = default);

        // Internal operations (clearly marked) - only used by domain services
        Task<Resource?> GetResourceInternalAsync(Guid resourceId, CancellationToken cancellationToken = default);
        Task UpdateResourceInternalAsync(Guid resourceId, Func<Resource, Resource> updateFn, CancellationToken cancellationToken = default);
    }
}
```

### Repository Implementation

Implement the repository with authorization checks integrated into every method:

```csharp
namespace YourProject.Infrastructure.Repositories
{
    using YourProject.Domain.Resources;
    using YourProject.Domain.Security;
    using Microsoft.EntityFrameworkCore;

    public class ResourceRepository : IResourceRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResourceRepository> _logger;

        public ResourceRepository(ApplicationDbContext context, ILogger<ResourceRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Resource?> GetResourceAsync(
            Guid resourceId,
            UserContext user,
            CancellationToken cancellationToken = default)
        {
            // First, retrieve the resource
            var resource = await GetResourceByIdAsync(resourceId, cancellationToken);
            if (resource == null)
            {
                return null;
            }

            // Apply domain authorization rule
            ResourceSecurityRules.CanUserViewResource(user, resource);

            _logger.LogInformation("User {UserId} accessed training {TrainingId}",
                user.UserId, trainingId);

            return training;
        }
        public async Task UpdateTrainingAsync(
            Guid trainingId,
            UserContext user,
            Func<Training, Training> updateFn,
            CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Get current training
                var training = await GetTrainingByIdAsync(trainingId, cancellationToken);
                if (training == null)
                {
                    throw new TrainingNotFoundException(trainingId);
                }

                // Check authorization before allowing updates
                TrainingSecurityRules.CanUserModifyTraining(user, training);

                // Apply the update function
                var updatedTraining = updateFn(training);

                // Save the updated training
                _context.Trainings.Update(updatedTraining);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("User {UserId} updated training {TrainingId}",
                    user.UserId, trainingId);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        // Private helper methods
        private async Task<Training?> GetTrainingByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Trainings
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }
    }
}
```

### Application Layer Integration

The application layer uses the secure repository without additional security checks:

```csharp
namespace Jacopo.Application.Training.Commands
{
    using MediatR;
    using Jacopo.Domain.Training;
    using Jacopo.Domain.Security;

    public class UpdateTrainingHandler : IRequestHandler<UpdateTrainingCommand, Unit>
    {
        private readonly ITrainingRepository _trainingRepository;
        private readonly ICurrentUserService _currentUserService;

        public UpdateTrainingHandler(
            ITrainingRepository trainingRepository,
            ICurrentUserService currentUserService)
        {
            _trainingRepository = trainingRepository;
            _currentUserService = currentUserService;
        }

        public async Task<Unit> Handle(UpdateTrainingCommand request, CancellationToken cancellationToken)
        {
            var user = await _currentUserService.GetCurrentUserAsync();

            // Repository handles all authorization - no additional checks needed here
            await _trainingRepository.UpdateTrainingAsync(
                request.TrainingId,
                user,
                training => training.UpdateDetails(request.Name, request.Description),
                cancellationToken);

            return Unit.Value;
        }
    }
}

type ApproveTrainingHandler struct {
    trainingRepo domain.TrainingRepository
}

func (h *ApproveTrainingHandler) Handle(ctx context.Context, cmd ApproveTrainingCommand) error {
    // No additional security checks needed - repository handles it
    return h.trainingRepo.UpdateTraining(
        ctx,
        cmd.TrainingID,
        cmd.User, // User context passed through

```

## Collection Security

### Query-Level Security

For collection operations, implement security at the query level rather than filtering results in application code:

```csharp
namespace Jacopo.Infrastructure.Repositories
{
    public partial class TrainingRepository
    {
        public async Task<IReadOnlyList<Training>> FindTrainingsForUserAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Trainings
                .Where(t => t.OwnerId == userId || t.TrainerIds.Contains(userId))
                .Where(t => !t.IsCanceled)
                .Where(t => t.ScheduledAt >= DateTime.UtcNow.AddHours(-24))
                .OrderByDescending(t => t.ScheduledAt)
                .ToListAsync(cancellationToken);
        }

        // Separate method for trainers to see all trainings they're assigned to
        public async Task<IReadOnlyList<Training>> FindTrainingsForTrainerAsync(
            string trainerId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Trainings
                .Where(t => t.TrainerIds.Contains(trainerId))
                .Where(t => !t.IsCanceled)
                .OrderBy(t => t.ScheduledAt)
                .ToListAsync(cancellationToken);
        }

        // Advanced query with user context for complex authorization
        public async Task<IReadOnlyList<Training>> FindAccessibleTrainingsAsync(
            UserContext user,
            TrainingFilter filter,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Trainings.AsQueryable();

            // Apply security filter based on user context
            if (user.Role != UserRole.SystemAdmin)
            {
                // Users can only see trainings they own, are trainers for, or have permission to view
                query = query.Where(t =>
                    t.TenantId == user.TenantId && (
                        t.OwnerId == user.UserId ||
                        t.TrainerIds.Contains(user.UserId) ||
                        user.HasPermission(Permission.ViewAllTrainings)
                    ));
            }

            // Apply additional filters
            if (filter.StartDate.HasValue)
                query = query.Where(t => t.ScheduledAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(t => t.ScheduledAt <= filter.EndDate.Value);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
                query = query.Where(t => t.Name.Contains(filter.SearchTerm) ||
                                       t.Description.Contains(filter.SearchTerm));

            return await query
                .OrderByDescending(t => t.ScheduledAt)
                .Take(filter.MaxResults)
                .ToListAsync(cancellationToken);
        }
    }

    public class TrainingFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; }
        public int MaxResults { get; set; } = 100;
    }
}
```

### Role-Based Collection Access

Create specific methods for different user roles rather than generic "get all" methods:

```csharp
// Good: Role-specific methods
public interface ITrainingRepository
{
    Task<IReadOnlyList<Training>> FindTrainingsForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Training>> FindTrainingsForTrainerAsync(string trainerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Training>> FindAllTrainingsForAdminAsync(AdminFilters filters, CancellationToken cancellationToken = default);
}

// Avoid: Generic method that requires external authorization
// Task<IReadOnlyList<Training>> FindAllTrainingsAsync(UserContext user, CancellationToken cancellationToken = default);
```

## Internal Operations

### Administrative Operations

For internal operations (migrations, admin functions, background jobs), create
explicitly named methods that make their purpose clear:

```csharp
// Clearly marked internal operations
public interface ITrainingRepository
{
    // Regular user operations
    Task<Training?> GetTrainingAsync(Guid trainingId, UserContext user, CancellationToken cancellationToken = default);

    // Internal operations - naming makes security implications clear
    Task<Training?> GetTrainingInternalAsync(Guid trainingId, CancellationToken cancellationToken = default);
    Task UpdateTrainingForMigrationAsync(Guid trainingId, Func<Training, Training> updateFn, CancellationToken cancellationToken = default);
    Task DeleteTrainingForCleanupAsync(Guid trainingId, CancellationToken cancellationToken = default);

    // System operations with explicit naming
    Task<Training?> GetTrainingForNotificationAsync(Guid trainingId, CancellationToken cancellationToken = default);
    Task UpdateTrainingBySystemAsync(Guid trainingId, string reason, Func<Training, Training> updateFn, CancellationToken cancellationToken = default);
}
```

### Background Job Security

For background operations, create specific command types and handlers:

```csharp
namespace Jacopo.Application.Training.Commands
{
    public class SystemUpdateTrainingCommand : IRequest
    {
        public required Guid TrainingId { get; init; }
        public required string Reason { get; init; }
        public required string SystemUserId { get; init; }
    }

    public class SystemUpdateTrainingHandler : IRequestHandler<SystemUpdateTrainingCommand>
    {
        private readonly ITrainingRepository _trainingRepository;
        private readonly ILogger<SystemUpdateTrainingHandler> _logger;

        public SystemUpdateTrainingHandler(
            ITrainingRepository trainingRepository,
            ILogger<SystemUpdateTrainingHandler> logger)
        {
            _trainingRepository = trainingRepository;
            _logger = logger;
        }

        public async Task Handle(SystemUpdateTrainingCommand request, CancellationToken cancellationToken)
        {
            // Use internal method that bypasses user authorization
            await _trainingRepository.UpdateTrainingBySystemAsync(
                request.TrainingId,
                request.Reason,
                training => training.UpdateSystemStatus(),
                cancellationToken);

            _logger.LogInformation("System updated training {TrainingId} for reason: {Reason}",
                request.TrainingId, request.Reason);
        }
    }
}
```

## Anti-Patterns to Avoid

### 1. Context-Based Authentication

**Avoid** passing authentication details via dependency injection context or ambient context:

```csharp
// Bad: Hidden authentication requirements using ambient context
public async Task<Training?> GetTrainingAsync(Guid trainingId)
{
    var user = _httpContext.User; // Loses explicit contract
    // Authorization logic mixed with data access
}

// Good: Explicit authentication requirements
public async Task<Training?> GetTrainingAsync(Guid trainingId, UserContext user)
{
    // Clear contract and type safety
}
```

### 2. External Authorization Checks

**Avoid** relying on callers to perform authorization:

```csharp
// Bad: Authorization responsibility placed on caller
if (!TrainingSecurityRules.CanUserViewTraining(user, training))
{
    throw new ForbiddenException();
}
var training = await repository.GetTrainingAsync(trainingId); // No user context!

// Good: Repository handles authorization internally
var training = await repository.GetTrainingAsync(trainingId, user);
```

### 3. Fake Users and Backdoors

**Avoid** creating fake users or bypassing authorization:

```csharp
// Bad: Creates maintenance and audit issues
var systemUser = new UserContext
{
    UserId = "system",
    Role = UserRole.SystemAdmin,
    TenantId = "system"
};
var training = await repository.GetTrainingAsync(trainingId, systemUser);

// Good: Explicit internal methods
var training = await repository.GetTrainingInternalAsync(trainingId);
```

### 4. Generic Repository Methods

**Avoid** overly generic methods that push authorization logic to callers:

```csharp
// Bad: Forces callers to implement authorization
Task<IReadOnlyList<Training>> FindAllAsync(Dictionary<string, object> filters);

// Good: Purpose-built methods with built-in security
Task<IReadOnlyList<Training>> FindTrainingsForUserAsync(string userId);
Task<IReadOnlyList<Training>> FindTrainingsForTrainerAsync(string trainerId);
```

## Testing Security

### Unit Testing Authorization Logic

Test domain authorization rules independently:

```csharp
namespace Jacopo.Domain.Tests.Security
{
    [TestFixture]
    public class TrainingSecurityRulesTests
    {
        [Test]
        public void CanUserViewTraining_TrainerCanViewAnyTraining()
        {
            // Arrange
            var trainer = new UserContext
            {
                UserId = "trainer1",
                Role = UserRole.Trainer,
                TenantId = "tenant1"
            };

            var training = new Training
            {
                Id = Guid.NewGuid(),
                OwnerId = "user2",
                TenantId = "tenant1",
                TrainerIds = new[] { "trainer2" }
            };

            // Act & Assert
            Assert.DoesNotThrow(() => TrainingSecurityRules.CanUserViewTraining(trainer, training));
        }

        [Test]
        public void CanUserViewTraining_UserCanViewOwnTraining()
        {
            // Arrange
            var user = new UserContext
            {
                UserId = "user1",
                Role = UserRole.User,
                TenantId = "tenant1"
            };

            var training = new Training
            {
                Id = Guid.NewGuid(),
                OwnerId = "user1",
                TenantId = "tenant1"
            };

            // Act & Assert
            Assert.DoesNotThrow(() => TrainingSecurityRules.CanUserViewTraining(user, training));
        }

        [Test]
        public void CanUserViewTraining_UserCannotViewOthersTraining()
        {
            // Arrange
            var user = new UserContext
            {
                UserId = "user1",
                Role = UserRole.User,
                TenantId = "tenant1"
            };

            var training = new Training
            {
                Id = Guid.NewGuid(),
                OwnerId = "user2",
                TenantId = "tenant1"
            };

            // Act & Assert
            Assert.Throws<ForbiddenToViewTrainingException>(() =>
                TrainingSecurityRules.CanUserViewTraining(user, training));
        }
    }
}
```

### Integration Testing Repository Security

Test that repositories properly enforce authorization:

```csharp
namespace Jacopo.Infrastructure.Tests.Repositories
{
    [TestFixture]
    public class TrainingRepositorySecurityTests
    {
        private ApplicationDbContext _context;
        private TrainingRepository _repository;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new TrainingRepository(_context, Mock.Of<ILogger<TrainingRepository>>());
        }

        [Test]
        public async Task GetTrainingAsync_UserCanAccessOwnTraining()
        {
            // Arrange
            var training = await CreateTestTrainingAsync("user1", "trainer1");
            var user = new UserContext
            {
                UserId = "user1",
                Role = UserRole.User,
                TenantId = "tenant1"
            };

            // Act
            var result = await _repository.GetTrainingAsync(training.Id, user);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(training.Id));
        }

        [Test]
        public async Task GetTrainingAsync_UserCannotAccessOthersTraining()
        {
            // Arrange
            var training = await CreateTestTrainingAsync("user1", "trainer1");
            var user = new UserContext
            {
                UserId = "user2",
                Role = UserRole.User,
                TenantId = "tenant1"
            };

            // Act & Assert
            Assert.ThrowsAsync<ForbiddenToViewTrainingException>(
                async () => await _repository.GetTrainingAsync(training.Id, user));
        }

        [Test]
        public async Task GetTrainingAsync_TrainerCanAccessAnyTraining()
        {
            // Arrange
            var training = await CreateTestTrainingAsync("user1", "trainer1");
            var trainer = new UserContext
            {
                UserId = "trainer2",
                Role = UserRole.Trainer,
                TenantId = "tenant1"
            };

            // Act
            var result = await _repository.GetTrainingAsync(training.Id, trainer);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(training.Id));
        }

        private async Task<Training> CreateTestTrainingAsync(string ownerId, string trainerId)
        {
            var training = new Training
            {
                Id = Guid.NewGuid(),
                Name = "Test Training",
                OwnerId = ownerId,
                TenantId = "tenant1",
                TrainerIds = new[] { trainerId }
            };

            _context.Trainings.Add(training);
            await _context.SaveChangesAsync();
            return training;
        }
    }
}
```

### Security Regression Testing

Create tests that verify security cannot be bypassed:

```csharp
namespace Jacopo.Infrastructure.Tests.Security
{
    [TestFixture]
    public class RepositorySecurityRegressionTests
    {
        [Test]
        public void CannotAccessTrainingWithoutUserContext()
        {
            // This test ensures we don't accidentally create methods that bypass authorization
            var repositoryType = typeof(TrainingRepository);
            var publicMethods = repositoryType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            foreach (var method in publicMethods)
            {
                // Skip methods that are explicitly internal or system methods
                if (method.Name.Contains("Internal") || method.Name.Contains("System"))
                    continue;

                // Any method that returns Training should require UserContext parameter
                var hasTrainingReturn = method.ReturnType.Name.Contains("Training") ||
                                      method.ReturnType.GetGenericArguments().Any(t => t.Name.Contains("Training"));

                var hasUserParameter = method.GetParameters().Any(p => p.ParameterType == typeof(UserContext));

                if (hasTrainingReturn && !hasUserParameter)
                {
                    Assert.Fail($"Method {method.Name} might bypass authorization - returns Training but doesn't require UserContext");
                }
            }
        }

        [Test]
        public async Task VerifyInternalMethodsAreNotPubliclyAccessible()
        {
            // Ensure internal methods are properly isolated
            var interfaceType = typeof(ITrainingRepository);
            var methods = interfaceType.GetMethods();

            var internalMethods = methods.Where(m => m.Name.Contains("Internal")).ToList();

            // Internal methods should not be on the public interface
            Assert.That(internalMethods, Has.Count.EqualTo(0),
                "Internal methods should not be exposed on public repository interface");
        }
    }
}
```

## Advanced Patterns

### Row-Level Security (RLS)

For PostgreSQL, consider implementing Row-Level Security policies:

```sql
-- Enable RLS on the trainings table
ALTER TABLE trainings ENABLE ROW LEVEL SECURITY;

-- Policy for users to see only their own trainings or where they are trainers
CREATE POLICY user_trainings_policy ON trainings
    FOR ALL TO app_user
    USING (owner_id = current_setting('app.user_id') OR
           trainer_ids @> ARRAY[current_setting('app.user_id')]);

-- Policy for trainers to see all trainings in their tenant
CREATE POLICY trainer_trainings_policy ON trainings
    FOR ALL TO app_trainer
    USING (tenant_id = current_setting('app.tenant_id'));
```

Then set the user context in your application:

```csharp
public class TrainingRepository : ITrainingRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<Training?> GetTrainingAsync(
        Guid trainingId,
        UserContext user,
        CancellationToken cancellationToken = default)
    {
        // Set user context for RLS using connection string parameters
        await _context.Database.ExecuteSqlRawAsync(
            "SET app.user_id = {0}; SET app.tenant_id = {1}",
            user.UserId, user.TenantId);

        // Query will automatically filter based on RLS policies
        return await _context.Trainings
            .FirstOrDefaultAsync(t => t.Id == trainingId, cancellationToken);
    }
}
```

### Audit Logging

Implement comprehensive audit logging for security-sensitive operations:

```csharp
namespace Jacopo.Infrastructure.Security
{
    public interface IAuditLogger
    {
        Task LogAccessAsync(UserContext user, string resource, string action, string result,
            CancellationToken cancellationToken = default);
    }

    public class TrainingRepository : ITrainingRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public TrainingRepository(ApplicationDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<Training?> GetTrainingAsync(
            Guid trainingId,
            UserContext user,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var training = await GetTrainingByIdAsync(trainingId, cancellationToken);
                if (training == null)
                {
                    await _auditLogger.LogAccessAsync(user, $"training:{trainingId}", "read", "not_found", cancellationToken);
                    return null;
                }

                // Apply domain authorization rule
                TrainingSecurityRules.CanUserViewTraining(user, training);

                await _auditLogger.LogAccessAsync(user, $"training:{trainingId}", "read", "success", cancellationToken);
                return training;
            }
            catch (ForbiddenToViewTrainingException)
            {
                await _auditLogger.LogAccessAsync(user, $"training:{trainingId}", "read", "forbidden", cancellationToken);
                throw;
            }
            catch (Exception)
            {
                await _auditLogger.LogAccessAsync(user, $"training:{trainingId}", "read", "error", cancellationToken);
                throw;
            }
        }
    }
}
```

## Conclusion

Security-first repository design creates systems that are inherently secure and
impossible to use incorrectly. By following these patterns:

1. **Build authorization into repository interfaces** - Make it impossible to forget security checks
2. **Model security as domain logic** - Security rules should reflect business requirements
3. **Use explicit naming for internal operations** - Make security implications clear
4. **Avoid context-based authentication** - Maintain type safety and explicit dependencies
5. **Test security thoroughly** - Include both positive and negative security test cases

This approach results in repositories that are secure by design, making your .NET
9 application more robust and your team more confident when making changes.

## References

- [Repository secure by design - Three Dots Labs](https://threedots.tech/post/repository-secure-by-design/)
- [Clean Architecture in .NET - Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/architecture/clean-architecture/)
- [Entity Framework Core Security Considerations](https://docs.microsoft.com/en-us/ef/core/miscellaneous/security/)
- [ASP.NET Core Security Overview](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [OSSF Security Baselines](https://github.com/ossf/security-baselines)
