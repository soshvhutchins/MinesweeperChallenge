# .NET 8 Compatibility Guide

## Overview

This document provides a comprehensive guide for migrating the Minesweeper project from .NET 9 to .NET 8. The project has **excellent .NET 8 compatibility** with only minor package version adjustments needed.

## Compatibility Assessment ✅

### What's Already Compatible

#### 🔤 **Language Features**

- ✅ **Records**: All used record types (`GameId`, `PlayerId`, request models) are supported in .NET 8
- ✅ **Init-only properties**: `{ get; init; }` syntax is .NET 5+ compatible
- ✅ **Nullable reference types**: Fully supported in .NET 8
- ✅ **File-scoped namespaces**: Using traditional namespaces (no file-scoped syntax used)
- ✅ **Implicit usings**: `<ImplicitUsings>enable</ImplicitUsings>` is .NET 6+ compatible

#### 🏗️ **Architecture Patterns**

- ✅ **Clean Architecture**: Framework-agnostic pattern
- ✅ **CQRS with MediatR**: All MediatR versions are .NET 8 compatible
- ✅ **Entity Framework Core**: EF Core 8.x versions work with .NET 8
- ✅ **ASP.NET Core**: All API patterns are .NET 8 compatible
- ✅ **Domain-Driven Design**: All DDD patterns are framework-agnostic
- ✅ **Testing Infrastructure**: xUnit, FluentAssertions, NSubstitute all support .NET 8

#### 🎮 **Game Logic**

- ✅ **Minesweeper algorithms**: Mine placement, flood-fill, game state management
- ✅ **Value objects**: GameDifficulty, CellPosition, Game entities
- ✅ **Domain events**: All event patterns are .NET 8 compatible
- ✅ **Game controllers**: RESTful API endpoints work identically

## Required Changes 🔧

### 1. Target Framework Updates

Update the `TargetFramework` in all `.csproj` files:

```xml
<!-- Change in all 6 .csproj files -->
<TargetFramework>net9.0</TargetFramework>
↓
<TargetFramework>net8.0</TargetFramework>
```

**Files to update:**

- `src/Minesweeper.Domain/Minesweeper.Domain.csproj`
- `src/Minesweeper.Application/Minesweeper.Application.csproj`
- `src/Minesweeper.Infrastructure/Minesweeper.Infrastructure.csproj`
- `src/Minesweeper.WebApi/Minesweeper.WebApi.csproj`
- `tests/Minesweeper.UnitTests/Minesweeper.UnitTests.csproj`
- `tests/Minesweeper.IntegrationTests/Minesweeper.IntegrationTests.csproj`

### 2. Package Version Updates

#### Microsoft Packages (Require Downgrade)

| Package                                 | .NET 9 Version | .NET 8 Compatible |
| --------------------------------------- | -------------- | ----------------- |
| `Microsoft.EntityFrameworkCore`         | 9.0.8          | **8.0.11**        |
| `Microsoft.EntityFrameworkCore.Design`  | 9.0.8          | **8.0.11**        |
| `Microsoft.EntityFrameworkCore.Sqlite`  | 9.0.8          | **8.0.11**        |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 9.0.4          | **8.0.8**         |
| `Microsoft.AspNetCore.OpenApi`          | 9.0.7          | **8.0.11**        |
| `Microsoft.AspNetCore.Mvc.Testing`      | 9.0.8          | **8.0.11**        |
| `Swashbuckle.AspNetCore`                | 9.0.3          | **8.0.1**         |

#### Third-Party Packages (Already Compatible) ✅

| Package                                          | Current Version | .NET 8 Status |
| ------------------------------------------------ | --------------- | ------------- |
| `MediatR`                                        | 13.0.0          | ✅ Compatible  |
| `AutoMapper`                                     | 15.0.1          | ✅ Compatible  |
| `FluentValidation`                               | 12.0.0          | ✅ Compatible  |
| `FluentValidation.DependencyInjectionExtensions` | 12.0.0          | ✅ Compatible  |
| `xunit`                                          | 2.9.2           | ✅ Compatible  |
| `xunit.runner.visualstudio`                      | 2.8.2 / 3.1.3   | ✅ Compatible  |
| `FluentAssertions`                               | 8.5.0           | ✅ Compatible  |
| `NSubstitute`                                    | 5.3.0           | ✅ Compatible  |
| `coverlet.collector`                             | 6.0.2           | ✅ Compatible  |
| `Testcontainers.PostgreSql`                      | 4.6.0           | ✅ Compatible  |
| `Microsoft.NET.Test.Sdk`                         | 17.12.0         | ✅ Compatible  |
| `Microsoft.AspNetCore.SignalR`                   | 1.2.0           | ✅ Compatible  |

## Migration Steps 🚀

### Step 1: Automated Target Framework Update

```bash
# Navigate to project root
cd /path/to/minesweeper/project

# Update all .csproj files (Unix/Linux/macOS)
find . -name "*.csproj" -exec sed -i 's/net9.0/net8.0/g' {} \;

# Windows PowerShell alternative
Get-ChildItem -Recurse -Filter "*.csproj" | ForEach-Object {
    (Get-Content $_.FullName) -replace "net9.0", "net8.0" | Set-Content $_.FullName
}
```

### Step 2: Update Package Versions

#### Infrastructure Project

Update `src/Minesweeper.Infrastructure/Minesweeper.Infrastructure.csproj`:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.11" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.11" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.8" />
```

#### Web API Project

Update `src/Minesweeper.WebApi/Minesweeper.WebApi.csproj`:

```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.11" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
<PackageReference Include="Swashbuckle.AspNetCore" Version="8.0.1" />
```

#### Integration Tests Project

Update `tests/Minesweeper.IntegrationTests/Minesweeper.IntegrationTests.csproj`:

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.11" />
```

### Step 3: Verification Commands

```bash
# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build

# Run all tests
dotnet test

# Run specific project tests
dotnet test tests/Minesweeper.UnitTests/
dotnet test tests/Minesweeper.IntegrationTests/

# Run the application
dotnet run --project src/Minesweeper.WebApi
```

## Expected Results 🎯

### ✅ **100% Code Compatibility**

- **No C# code changes required**
- All language features used are .NET 8 compatible
- Records, init-only properties, nullable reference types work identically

### ✅ **Full Feature Parity**

- **Game Logic**: Mine placement, cell revealing, flood-fill algorithm
- **Clean Architecture**: All layer separations remain intact
- **CQRS Pattern**: Commands and queries work identically
- **Entity Framework**: Database operations, migrations, configurations
- **Testing**: Unit tests, integration tests, mocking frameworks
- **API Features**: Swagger documentation, OpenAPI, controllers

### ✅ **Performance Expectations**

- **Minimal differences** between .NET 8 and .NET 9 for this project
- **Game performance**: Identical response times and throughput
- **Database performance**: EF Core 8.x provides equivalent performance
- **API performance**: ASP.NET Core 8.x maintains same performance characteristics

### ✅ **Development Experience**

- **VS Code tasks**: All tasks work identically with .NET 8 SDK
- **Debugging**: Full debugging support maintained
- **Hot reload**: Available in .NET 8
- **Swagger UI**: Identical API documentation experience

## Validation Checklist ✔️

After migration, verify these key areas:

### Compilation

- [ ] `dotnet build` succeeds for all projects
- [ ] No compilation warnings related to framework version
- [ ] All NuGet packages restore successfully

### Testing

- [ ] All unit tests pass (`dotnet test tests/Minesweeper.UnitTests/`)
- [ ] All integration tests pass (`dotnet test tests/Minesweeper.IntegrationTests/`)
- [ ] Test coverage remains consistent

### Game Functionality

- [ ] Game creation works correctly
- [ ] Cell revealing logic functions properly
- [ ] Mine placement and flood-fill algorithms work
- [ ] Win/loss detection operates correctly
- [ ] All difficulty levels function properly

### API Functionality

- [ ] Swagger UI loads correctly at `/swagger`
- [ ] All API endpoints respond correctly
- [ ] Game CRUD operations work
- [ ] Error handling and validation function properly

### Database Operations

- [ ] Entity Framework migrations work
- [ ] Database connections establish successfully
- [ ] Game state persistence functions correctly
- [ ] Both SQLite and PostgreSQL connections work

## Troubleshooting 🔧

### Common Issues and Solutions

#### Package Restore Issues

```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore --force
```

#### EF Core Migration Issues

```bash
# Remove existing migrations if needed
dotnet ef migrations remove --project src/Minesweeper.Infrastructure --startup-project src/Minesweeper.WebApi

# Recreate initial migration
dotnet ef migrations add InitialCreate --project src/Minesweeper.Infrastructure --startup-project src/Minesweeper.WebApi

# Update database
dotnet ef database update --project src/Minesweeper.Infrastructure --startup-project src/Minesweeper.WebApi
```

#### VS Code Task Issues

- Ensure .NET 8 SDK is installed
- Update VS Code C# extension if needed
- Restart VS Code after SDK installation

## Benefits of .NET 8 Migration 📈

### Stability

- **LTS Release**: .NET 8 is a Long-Term Support release
- **Production Ready**: Mature, well-tested framework
- **Extended Support**: Supported until November 2026

### Educational Value

- **Broader Compatibility**: More students/developers have .NET 8 installed
- **Enterprise Adoption**: Many organizations standardize on LTS releases
- **Learning Relevance**: Most tutorials and examples target .NET 8

### Deployment

- **Docker Images**: Smaller, more optimized .NET 8 runtime images
- **Cloud Compatibility**: Better support across cloud providers
- **Legacy System Integration**: Easier integration with existing .NET 8 systems

## Conclusion 🎉

The Minesweeper project can be **seamlessly migrated to .NET 8** with:

- **Minimal effort**: Just target framework and package version changes
- **Zero code changes**: All architectural patterns and game logic remain identical
- **Full functionality**: Complete feature parity maintained
- **Enhanced compatibility**: Better support for educational and enterprise environments

The migration preserves all the educational value while improving accessibility for a broader audience of .NET developers.

---

*Last updated: August 5, 2025*
*Minesweeper v1.0.0 - Clean Architecture with .NET 8 Compatibility*
