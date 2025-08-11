# C4 Architecture Diagramming in .NET 9 Projects

This guide provides practical guidance for generating and maintaining C4 architecture
diagrams in .NET 9 projects, specifically using Structurizr for .NET and following
Clean Architecture principles.

## Overview

C4 diagramming is a visual approach to describing software architecture through
four different levels of abstraction:

- **Level 1: System Context** - Shows how your software system fits into the world around it
- **Level 2: Container** - Zooms into your system to show containers
  (applications, microservices, databases)
- **Level 3: Component** - Zooms into an individual container to show components
- **Level 4: Code** - Zooms into an individual component to show implementation details

This guide focuses primarily on Level 3 (Component) diagrams, which are most relevant
for documenting Clean Architecture implementations in .NET 9.

## Why Auto-Generated C4 Diagrams?

### Advantages

- **Always Up-to-Date**: Diagrams reflect the current codebase state
- **Consistency**: Standardized representation across teams and projects
- **Reduced Maintenance**: No manual diagram updates required
- **Clean Architecture Alignment**: Visualizes dependency inversion and layering
- **Documentation as Code**: Diagrams live alongside implementation

### Use Cases

- Architecture reviews and discussions
- Onboarding new team members
- System documentation for stakeholders
- Refactoring planning and validation
- Dependency analysis and cleanup

## Getting Started with Structurizr for .NET

### Installation

```bash
dotnet add package Structurizr.Core
dotnet add package Structurizr.PlantUML
```

### Basic Implementation

1. **Define Software System and Containers**

Create your workspace and define the high-level architecture:

```csharp
using Structurizr;

namespace YourProject.Documentation;

public class ArchitectureDiagramGenerator
{
    public static void GenerateDiagrams()
    {
        var workspace = new Workspace("Your Application",
            "Enterprise application architecture");

        var model = workspace.Model;

        // Define the software system
        var yourSystem = model.AddSoftwareSystem("Your Application",
            "Description of your application's purpose");

        // Define containers
        var webApp = yourSystem.AddContainer("Web Application",
            "Web interface for user interactions", "ASP.NET Core/.NET 9");
        var domainLayer = yourSystem.AddContainer("Domain Layer",
            "Core business logic and entities", ".NET 9/Clean Architecture");
        var applicationLayer = yourSystem.AddContainer("Application Layer",
            "Use cases and workflows", ".NET 9/MediatR");
        var infrastructureLayer = yourSystem.AddContainer("Infrastructure Layer",
            "External services integration", ".NET 9/Entity Framework");

        // Define relationships
        webApp.Uses(applicationLayer, "Sends commands and queries");
        applicationLayer.Uses(domainLayer, "Orchestrates business logic");
        applicationLayer.Uses(infrastructureLayer, "Persists data and calls external APIs");
        infrastructureLayer.Uses(domainLayer, "Implements domain interfaces");

        // Create views
        var views = workspace.Views;

        var containerView = views.CreateContainerView(yourSystem,
            "YourAppContainers", "Container view of Your Application");
        containerView.AddAllContainers();

        // Export to PlantUML
        var plantUMLWriter = new PlantUMLWriter();
        var diagrams = plantUMLWriter.Write(workspace);

        File.WriteAllText("architecture-containers.puml", diagrams);
    }
}
```

### Component-Level Modeling

Model individual components within containers:

```csharp
public static void GenerateComponentDiagram()
{
    var workspace = new Workspace("Jacopo Components",
        "Component view of Jacopo application layer");

    var model = workspace.Model;
    var jacopoSystem = model.AddSoftwareSystem("Jacopo", "Migration tool");
    var applicationLayer = jacopoSystem.AddContainer("Application Layer",
        "CQRS commands and queries", ".NET 9");

    // Define components
    var migrationSessionHandler = applicationLayer.AddComponent("MigrationSessionHandler",
        "Handles migration session lifecycle", "MediatR Command Handler");
    var migrationUserHandler = applicationLayer.AddComponent("MigrationUserHandler",
        "Manages user migration operations", "MediatR Command Handler");
    var graphService = applicationLayer.AddComponent("GraphService",
        "Microsoft Graph API integration", "Service");
    var sessionRepository = applicationLayer.AddComponent("SessionRepository",
        "Migration session persistence", "Repository Pattern");

    // Define relationships
    migrationSessionHandler.Uses(sessionRepository, "Persists session data");
    migrationUserHandler.Uses(graphService, "Calls Microsoft Graph API");
    migrationUserHandler.Uses(sessionRepository, "Updates session status");

    // Create component view
    var views = workspace.Views;
    var componentView = views.CreateComponentView(applicationLayer,
        "ApplicationComponents", "Application layer components");
    componentView.AddAllComponents();

    // Style components by type
    componentView.SetComponentStyle("MediatR Command Handler")
        .Background("#87CEEB")
        .Color("#000000");
    componentView.SetComponentStyle("Service")
        .Background("#98FB98")
        .Color("#000000");
    componentView.SetComponentStyle("Repository Pattern")
        .Background("#FFD700")
        .Color("#000000");

    var plantUMLWriter = new PlantUMLWriter();
    var diagrams = plantUMLWriter.Write(workspace);

    File.WriteAllText("architecture-components.puml", diagrams);
}
```

### Clean Architecture Integration

#### Layer-Based Component Modeling

Use consistent styling and organization to represent Clean Architecture layers:

```csharp
public static void GenerateLayeredArchitectureDiagram()
{
    var workspace = new Workspace("Jacopo Clean Architecture",
        "Clean Architecture layers in Jacopo");

    var model = workspace.Model;
    var jacopoSystem = model.AddSoftwareSystem("Jacopo", "Migration tool");

    // Define containers for each layer
    var domainLayer = jacopoSystem.AddContainer("Domain Layer",
        "Core business logic and entities", ".NET 9");
    var applicationLayer = jacopoSystem.AddContainer("Application Layer",
        "Use cases and workflows", ".NET 9/MediatR");
    var infrastructureLayer = jacopoSystem.AddContainer("Infrastructure Layer",
        "External services and persistence", ".NET 9");
    var presentationLayer = jacopoSystem.AddContainer("Presentation Layer",
        "CLI interface", "Spectre.Console/.NET 9");

    // Domain Layer Components
    var migrationSession = domainLayer.AddComponent("MigrationSession",
        "Core migration session aggregate root", "Domain Entity");
    var migrationUser = domainLayer.AddComponent("MigrationUser",
        "User migration aggregate root", "Domain Entity");
    var emailAddress = domainLayer.AddComponent("EmailAddress",
        "Validated email value object", "Value Object");
    var tenantId = domainLayer.AddComponent("TenantId",
        "Tenant identifier value object", "Value Object");

    // Application Layer Components
    var createSessionHandler = applicationLayer.AddComponent("CreateMigrationSessionHandler",
        "Handles migration session creation", "Command Handler");
    var migrateCalendarHandler = applicationLayer.AddComponent("MigrateCalendarHandler",
        "Handles calendar migration", "Command Handler");
    var sessionQueries = applicationLayer.AddComponent("MigrationSessionQueries",
        "Session query handlers", "Query Handler");

    // Infrastructure Layer Components
    var graphService = infrastructureLayer.AddComponent("MicrosoftGraphService",
        "Microsoft Graph API integration", "Service");
    var sessionRepository = infrastructureLayer.AddComponent("MigrationSessionRepository",
        "Session persistence", "Repository");
    var dbContext = infrastructureLayer.AddComponent("JacopoDbContext",
        "Entity Framework context", "Database Context");

    // Presentation Layer Components
    var cliApp = presentationLayer.AddComponent("Program",
        "CLI application entry point", "Console Application");
    var sessionMenu = presentationLayer.AddComponent("SessionMenu",
        "Session management menu", "CLI Menu");

    // Define relationships following Clean Architecture rules
    // Presentation -> Application
    sessionMenu.Uses(createSessionHandler, "Sends commands");
    sessionMenu.Uses(sessionQueries, "Sends queries");

    // Application -> Domain
    createSessionHandler.Uses(migrationSession, "Creates sessions");
    migrateCalendarHandler.Uses(migrationUser, "Updates user state");
    createSessionHandler.Uses(emailAddress, "Validates emails");

    // Application -> Infrastructure (through interfaces)
    createSessionHandler.Uses(sessionRepository, "Persists sessions");
    migrateCalendarHandler.Uses(graphService, "Calls Graph API");

    // Infrastructure -> Domain (implementing interfaces)
    sessionRepository.Uses(migrationSession, "Persists domain entities");
    graphService.Uses(emailAddress, "Uses domain types");

    // Create views with Clean Architecture styling
    var views = workspace.Views;

    var systemView = views.CreateSystemContextView(jacopoSystem,
        "SystemContext", "System context for Jacopo");
    systemView.AddAllSoftwareSystems();

    var containerView = views.CreateContainerView(jacopoSystem,
        "Containers", "Clean Architecture containers");
    containerView.AddAllContainers();

    // Style layers with different colors
    containerView.SetContainerStyle("Domain Layer")
        .Background("#87CEEB")  // Sky blue
        .Color("#000000");

    containerView.SetContainerStyle("Application Layer")
        .Background("#98FB98")  // Pale green
        .Color("#000000");

    containerView.SetContainerStyle("Infrastructure Layer")
        .Background("#FFD700")  // Gold
        .Color("#000000");

    containerView.SetContainerStyle("Presentation Layer")
        .Background("#FFA07A")  // Light salmon
        .Color("#000000");

    // Component view for each layer
    var domainComponentView = views.CreateComponentView(domainLayer,
        "DomainComponents", "Domain layer components");
    domainComponentView.AddAllComponents();

    var applicationComponentView = views.CreateComponentView(applicationLayer,
        "ApplicationComponents", "Application layer components");
    applicationComponentView.AddAllComponents();

    // Export to PlantUML
    var plantUMLWriter = new PlantUMLWriter();
    var diagrams = plantUMLWriter.Write(workspace);

    File.WriteAllText("clean-architecture.puml", diagrams);
}
```

#### Configuration-Based Approach

For larger projects, use configuration-based diagram generation:

```csharp
public class DiagramConfiguration
{
    public string WorkspaceName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<LayerConfig> Layers { get; set; } = new();
    public List<ComponentConfig> Components { get; set; } = new();
    public List<RelationshipConfig> Relationships { get; set; } = new();
}

public class LayerConfig
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Technology { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public List<string> ComponentPatterns { get; set; } = new();
}

public class ComponentConfig
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Technology { get; set; } = string.Empty;
    public string Layer { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}

public static void GenerateFromConfiguration(string configPath)
{
    var json = File.ReadAllText(configPath);
    var config = JsonSerializer.Deserialize<DiagramConfiguration>(json);

    var workspace = new Workspace(config.WorkspaceName, config.Description);
    var model = workspace.Model;
    var system = model.AddSoftwareSystem("Jacopo", "Migration tool");

    // Create containers from layer configuration
    var containers = new Dictionary<string, Container>();
    foreach (var layer in config.Layers)
    {
        containers[layer.Name] = system.AddContainer(
            layer.Name, layer.Description, layer.Technology);
    }

    // Create components from configuration
    var components = new Dictionary<string, Component>();
    foreach (var comp in config.Components)
    {
        var container = containers[comp.Layer];
        components[comp.Name] = container.AddComponent(
            comp.Name, comp.Description, comp.Technology);
    }

    // Create relationships from configuration
    foreach (var rel in config.Relationships)
    {
        var source = components[rel.Source];
        var destination = components[rel.Destination];
        source.Uses(destination, rel.Description);
    }

    // Generate views and export
    var views = workspace.Views;
    var containerView = views.CreateContainerView(system, "Containers", "System containers");
    containerView.AddAllContainers();

    // Apply layer styling
    foreach (var layer in config.Layers)
    {
        if (!string.IsNullOrEmpty(layer.BackgroundColor))
        {
            containerView.SetContainerStyle(layer.Name)
                .Background(layer.BackgroundColor)
                .Color("#000000");
        }
    }

    var plantUMLWriter = new PlantUMLWriter();
    var diagrams = plantUMLWriter.Write(workspace);
    File.WriteAllText("configured-architecture.puml", diagrams);
}

## Best Practices

### 1. Component Naming and Description

- Use clear, descriptive component names that match your ubiquitous language
- Provide meaningful descriptions that explain the component's responsibility
- Include technology information to help understand implementation choices

```csharp
// Good
public class EmailNotificationService
{
    public ComponentInfo GetComponentInfo()
    {
        return new ComponentInfo(
            "EmailNotificationService",
            "Sends email notifications for user events using SMTP",
            ".NET Service + SMTP",
            new[] { "infrastructure", "notification" }
        );
    }
}

// Avoid
public class EmailNotificationService
{
    public ComponentInfo GetComponentInfo()
    {
        return new ComponentInfo(
            "EmailService",
            "Sends emails",
            "Service",
            new[] { "service" }
        );
    }
}
```

### 2. Consistent Tagging Strategy

Establish a consistent tagging strategy across your project:

```csharp
// Layer tags
public static class LayerTags
{
    public const string Domain = "domain";
    public const string Application = "application";
    public const string Interface = "interface";
    public const string Infrastructure = "infrastructure";
}

// Pattern tags
public static class PatternTags
{
    public const string Service = "service";
    public const string Repository = "repository";
    public const string Controller = "controller";
    public const string Handler = "handler";
    public const string Adapter = "adapter";
    public const string Gateway = "gateway";
}

// Technology tags
public static class TechnologyTags
{
    public const string HTTP = "http";
    public const string gRPC = "grpc";
    public const string Database = "database";
    public const string Messaging = "messaging";
    public const string Cache = "cache";
}
```

### 3. Dependency Visualization

Structure your application composition to clearly show dependencies:

```csharp
public class ApplicationComposition
{
    // Domain Layer
    public MigrationSessionService MigrationSessionService { get; }
    public MigrationUserService MigrationUserService { get; }

    // Application Layer
    public CreateMigrationSessionHandler CreateSessionHandler { get; }
    public MigrateCalendarHandler CalendarHandler { get; }

    // Interface Layer
    public SessionController SessionController { get; }
    public MigrationController MigrationController { get; }

    // Infrastructure Layer
    public IMigrationSessionRepository SessionRepository { get; }
    public IMicrosoftGraphService GraphService { get; }

    public ComponentInfo GetComponentInfo()
    {
        return new ComponentInfo(
            "ApplicationComposition",
            "Main application composition root with dependency injection",
            "Dependency Container",
            new[] { "root" }
        );
    }
}
```

### 4. Filtering and Views

Create focused views for different audiences:

```csharp
// Architecture overview - focus on main components
public static void GenerateArchitectureOverview()
{
    var workspace = new Workspace("Architecture Overview", "Main system components");
    // Configure with domain and application layer only
    var view = workspace.Views.CreateContainerView(system, "Overview", "Architecture overview");
    // Add filtering logic here
}

// Infrastructure details - focus on data layer
public static void GenerateInfrastructureView()
{
    var workspace = new Workspace("Infrastructure Layer", "Data and external services");
    // Configure with infrastructure components only
}

// API surface - focus on interface layer
public static void GenerateApiView()
{
    var workspace = new Workspace("API Surface", "External interfaces");
    // Configure with interface and application layers
}

## Integration with Build Process

### Makefile Integration

```makefile
.PHONY: docs-generate
docs-generate:
    @echo "Generating architecture diagrams..."
    @dotnet run --project tools/DiagramGenerator
    @plantuml -tpng docs/architecture.puml
    @echo "Diagrams generated in docs/ directory"

.PHONY: docs-validate
docs-validate: docs-generate
    @echo "Validating documentation is up-to-date..."
    @git diff --exit-code docs/ || (echo "Documentation is out of date. Run 'make docs-generate'" && exit 1)
```

### GitHub Actions Integration

```yaml
name: Documentation

on:
  pull_request:
    paths:
      - '**/*.cs'
      - 'docs/**'

jobs:
  validate-docs:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0'

      - name: Generate diagrams
        run: make docs-generate

      - name: Check if diagrams are up-to-date
        run: |
          if [ -n "$(git status --porcelain docs/)" ]; then
            echo "Documentation is out of date"
            git diff docs/
            exit 1
          fi
```

## Troubleshooting

### Common Issues

1. **Components Not Appearing**
   - Ensure components are properly registered in dependency injection
   - Check namespace and assembly patterns in configuration
   - Verify component classes have appropriate interfaces or attributes

2. **Circular Dependencies**
   - Structurizr handles circular dependencies gracefully
   - Use interfaces to break dependency cycles in your architecture
   - Consider if circular dependencies indicate design issues

3. **Too Many Components**
   - Use filtering and multiple focused diagrams
   - Create component groupings by feature or layer
   - Consider hierarchical views (system → container → component)

4. **Missing Relationships**
   - Ensure dependencies are properly injected via constructor injection
   - Use public properties or interfaces for dependencies you want to visualize
   - Check that dependency types are included in the workspace model

### Debug Mode

Enable detailed logging to troubleshoot generation issues:

```csharp
public static void Main(string[] args)
{
    var loggerFactory = LoggerFactory.Create(builder => builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Debug));

    var logger = loggerFactory.CreateLogger<ArchitectureDiagramGenerator>();

    try
    {
        GenerateArchitectureDiagrams();
        logger.LogInformation("Architecture diagrams generated successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to generate architecture diagrams");
    }
}
```

## Advanced Patterns

### Custom Component Discovery

```csharp
// Custom attribute for marking architectural components
[AttributeUsage(AttributeTargets.Class)]
public class ArchitecturalComponentAttribute : Attribute
{
    public string Layer { get; }
    public string Description { get; }
    public string Technology { get; }

    public ArchitecturalComponentAttribute(string layer, string description, string technology = ".NET 9")
    {
        Layer = layer;
        Description = description;
        Technology = technology;
    }
}

// Usage on classes
[ArchitecturalComponent("infrastructure", "External HTTP client for Microsoft Graph API", "HTTP Client")]
public class MicrosoftGraphService : IMicrosoftGraphService
{
    // Implementation
}

[ArchitecturalComponent("infrastructure", "Message queue integration for events", "Azure Service Bus")]
public class EventPublisher : IEventPublisher
{
    // Implementation
}

// Reflection-based component discovery
public static class ComponentDiscovery
{
    public static List<ComponentInfo> DiscoverComponents(Assembly assembly)
    {
        var components = new List<ComponentInfo>();

        var types = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<ArchitecturalComponentAttribute>() != null);

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<ArchitecturalComponentAttribute>()!;
            components.Add(new ComponentInfo(
                type.Name,
                attr.Description,
                attr.Technology,
                new[] { attr.Layer, DeterminePattern(type) }
            ));
        }

        return components;
    }

    private static string DeterminePattern(Type type)
    {
        if (type.Name.EndsWith("Repository")) return "repository";
        if (type.Name.EndsWith("Service")) return "service";
        if (type.Name.EndsWith("Handler")) return "handler";
        if (type.Name.EndsWith("Controller")) return "controller";
        return "component";
    }
}
```

### Dynamic Component Information

```csharp
public class DatabaseRepository<T> : IRepository<T> where T : class
{
    private readonly string _tableName;
    private readonly DbContext _context;

    public DatabaseRepository(DbContext context)
    {
        _context = context;
        _tableName = typeof(T).Name;
    }

    public ComponentInfo GetComponentInfo()
    {
        return new ComponentInfo(
            $"{_tableName}Repository",
            $"Entity Framework repository for {_tableName} entities",
            "EF Core Repository",
            new[] { "infrastructure", "repository", "database" }
        );
    }
}

// Usage with dependency injection
public void ConfigureServices(IServiceCollection services)
{
    services.AddScoped<IRepository<MigrationSession>, DatabaseRepository<MigrationSession>>();
    services.AddScoped<IRepository<MigrationUser>, DatabaseRepository<MigrationUser>>();
    // Other registrations...
}
```

## Conclusion

Auto-generated C4 diagrams using Structurizr for .NET provide a powerful way to visualize and
document .NET 9 applications following Clean Architecture principles. By implementing consistent
tagging strategies, meaningful component descriptions, and integrating diagram generation into
your build process, you can maintain always up-to-date architectural documentation that serves
both development teams and stakeholders.

Remember to:

- Start simple and evolve your diagramming approach
- Focus on the most important architectural relationships
- Use multiple focused views rather than one complex diagram
- Integrate diagram generation into your CI/CD pipeline
- Regularly review and refine your component descriptions and tags

For more detailed examples and advanced usage patterns, refer to the
[go-structurizr documentation](https://pkg.go.dev/github.com/krzysztofreczek/go-structurizr) and the
[Three Dots Labs article](https://threedots.tech/post/auto-generated-c4-architecture-diagrams-in-go/).
