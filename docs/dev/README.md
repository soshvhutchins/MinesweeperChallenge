# .NET/C# Architecture & Best Practices Documentation

## Table of Contents

- [.NET/C# Architecture \& Best Practices Documentation](#netc-architecture--best-practices-documentation)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Documents Overview](#documents-overview)
    - [📚 Core Architecture Concepts](#-core-architecture-concepts)
    - [🛠️ Development Practices](#️-development-practices)
    - [🗄️ Data \& Transactions](#️-data--transactions)
    - [🚀 DevOps \& CI/CD](#-devops--cicd)
  - [Suggested Reading Order](#suggested-reading-order)
    - [For Beginners to These Patterns](#for-beginners-to-these-patterns)
    - [For Active Development](#for-active-development)
    - [For Production \& Scale](#for-production--scale)
  - [Quick Reference](#quick-reference)
    - [Key Technologies Covered](#key-technologies-covered)
    - [Architecture Patterns](#architecture-patterns)
    - [Design Principles](#design-principles)
  - [Contributing](#contributing)
  - [Reference Projects](#reference-projects)
  - [Getting Help](#getting-help)
  - [Suggested External Reading](#suggested-external-reading)
    - [Books](#books)
      - [Architecture \& Design Patterns](#architecture--design-patterns)
      - [.NET Specific](#net-specific)
    - [Articles \& Documentation](#articles--documentation)
      - [Microsoft Official Documentation](#microsoft-official-documentation)
      - [Essential Articles \& Guides](#essential-articles--guides)
    - [Sample Projects \& Repositories](#sample-projects--repositories)
      - [.NET Architecture Samples](#net-architecture-samples)
      - [Testing Resources](#testing-resources)
    - [Courses \& Learning Platforms](#courses--learning-platforms)
      - [Online Courses](#online-courses)
      - [YouTube Channels](#youtube-channels)
    - [Blogs \& Websites](#blogs--websites)
      - [Architecture \& Design](#architecture--design)
      - [.NET Development](#net-development)
    - [Tools \& Libraries](#tools--libraries)
      - [Development Tools](#development-tools)
      - [Useful NuGet Packages](#useful-nuget-packages)
    - [Conferences \& Events](#conferences--events)
      - [Major .NET Conferences](#major-net-conferences)
      - [Local Meetups](#local-meetups)

This directory contains comprehensive documentation for building modern .NET/C#
applications using Domain-Driven Design (DDD), Command Query Responsibility
Segregation (CQRS), and Clean Architecture patterns.

## Overview

These documents have been specifically crafted for .NET 9+ development, incorporating
modern frameworks like Entity Framework Core, MediatR, and following Clean Architecture
principles. They serve as both human-readable guides and AI-assistant reference materials.

## Documents Overview

### 📚 Core Architecture Concepts

- **[Architecture Patterns Explained](./architecture_patterns_explained_dotnet.md)** - Beginner-friendly introduction to
  DDD, CQRS, and Clean Architecture
- **[Project Guidelines](./project_guidelines_dotnet.md)** - Comprehensive project structure and coding standards
- **[Software Principles](./software_principles_dotnet.md)** - SOLID principles and design patterns in .NET/C#

### 🛠️ Development Practices

- **[Copilot Instructions](./copilot_instructions_dotnet.md)** - AI coding assistant guidelines and best practices
- **[Testing Guide](./testing_dotnet.md)** - Complete testing strategy with xUnit, FluentAssertions, and TestContainers
- **[Security Best Practices](./security_dotnet.md)** - .NET security guidelines and implementation patterns

### 🗄️ Data & Transactions

- **[Database Transactions](./database_transactions_dotnet.md)** - Entity Framework Core transaction patterns and best practices
- **[Distributed Transactions](./distributed_transactions_dotnet.md)** - Event-driven architecture and distributed
  system patterns
- **[Idempotence & Atomicity](./idempotence_predictability_atomic_dotnet.md)** - Ensuring reliable and predictable operations

### 🚀 DevOps & CI/CD

- **[GitHub CI/CD](./github_cicd_dotnet.md)** - .NET-specific GitHub Actions workflows and deployment strategies

## Suggested Reading Order

### For Beginners to These Patterns

1. **Start Here**: [Architecture Patterns Explained](./architecture_patterns_explained_dotnet.md)
   - Get a high-level understanding of DDD, CQRS, and Clean Architecture
   - Learn the "why" behind these patterns

2. **Foundation**: [Software Principles](./software_principles_dotnet.md)
   - Understand SOLID principles and design patterns
   - See how they apply to .NET/C# development

3. **Project Structure**: [Project Guidelines](./project_guidelines_dotnet.md)
   - Learn how to organize your .NET projects
   - Understand naming conventions and folder structures

### For Active Development

1. **Development Workflow**: [Copilot Instructions](./copilot_instructions_dotnet.md)
   - Best practices for AI-assisted development
   - Code generation guidelines and patterns

2. **Testing Strategy**: [Testing Guide](./testing_dotnet.md)
   - Comprehensive testing approach
   - Unit, integration, and end-to-end testing patterns

3. **Data Handling**: [Database Transactions](./database_transactions_dotnet.md)
   - Entity Framework Core best practices
   - Transaction management and patterns

### For Production & Scale

1. **Security**: [Security Best Practices](./security_dotnet.md)
   - Authentication, authorization, and data protection
   - Security-first development practices

2. **Reliability**: [Idempotence & Atomicity](./idempotence_predictability_atomic_dotnet.md)
   - Building resilient and predictable systems
   - Handling failures gracefully

3. **Distributed Systems**: [Distributed Transactions](./distributed_transactions_dotnet.md)
   - Event-driven architecture patterns
   - Microservices communication strategies

4. **Deployment**: [GitHub CI/CD](./github_cicd_dotnet.md)
   - Automated testing and deployment
   - Production-ready CI/CD pipelines

## Quick Reference

### Key Technologies Covered

- **.NET 9+** - Modern .NET framework
- **Entity Framework Core** - Data access and ORM
- **MediatR** - CQRS and mediator patterns
- **xUnit** - Testing framework
- **FluentAssertions** - Assertion library
- **TestContainers** - Integration testing
- **ASP.NET Core** - Web API development
- **GitHub Actions** - CI/CD automation

### Architecture Patterns

- **Clean Architecture** - Dependency inversion and separation of concerns
- **Domain-Driven Design (DDD)** - Business-focused code organization
- **CQRS** - Command Query Responsibility Segregation
- **Event Sourcing** - Event-driven data persistence
- **Repository Pattern** - Data access abstraction
- **Unit of Work** - Transaction management

### Design Principles

- **SOLID Principles** - Object-oriented design fundamentals
- **DRY** - Don't Repeat Yourself
- **YAGNI** - You Aren't Gonna Need It
- **KISS** - Keep It Simple, Stupid
- **Fail Fast** - Early error detection and handling

## Contributing

When updating these documents:

1. Maintain consistency with .NET 9+ best practices
2. Include real-world examples and patterns
3. Focus on practical implementation guidance
4. Update cross-references between documents as needed
5. Follow markdown formatting standards

## Reference Projects

These documents are based on proven enterprise patterns and demonstrate:

- - Modern .NET 9+ Clean Architecture implementation
- Domain-driven design with tactical patterns
- CQRS with MediatR and FluentValidation
- Comprehensive testing strategies
- Security best practices
- Production-ready CI/CD workflows

## Getting Help

Each document includes:

- ✅ **Pros** - Benefits of using the pattern/practice
- ❌ **Cons** - Potential drawbacks and when not to use
- 🔍 **Examples** - Real-world .NET/C# code samples
- 📖 **References** - Additional resources and documentation

For specific implementation questions, refer to the relevant document's examples and proven enterprise architecture patterns.

## Suggested External Reading

### Books

#### Architecture & Design Patterns

- **"Clean Architecture"** by Robert C. Martin - Foundational principles of Clean Architecture
- **"Domain-Driven Design"** by Eric Evans - The original DDD blueprint
- **"Implementing Domain-Driven Design"** by Vaughn Vernon - Practical DDD implementation guide
- **"Patterns of Enterprise Application Architecture"** by Martin Fowler - Enterprise patterns
  including Repository and Unit of Work
- **"Building Microservices"** by Sam Newman - Distributed systems and microservices architecture

#### .NET Specific

- **".NET Microservices: Architecture for Containerized .NET Applications"** by Microsoft - Free eBook on .NET microservices
- **"C# in Depth"** by Jon Skeet - Advanced C# programming techniques
- **"Effective C#"** by Bill Wagner - Best practices for C# development
- **"Entity Framework Core in Action"** by Jon P Smith - Comprehensive EF Core guide

### Articles & Documentation

#### Microsoft Official Documentation

- [.NET Application Architecture Guides](https://docs.microsoft.com/en-us/dotnet/architecture/) - Microsoft's official
  architecture guidance
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/) - Complete EF Core reference
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/) - Web API and MVC guidance
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/) - Security best practices

#### Essential Articles & Guides

- [Clean Architecture Blog Series](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) by Uncle
  Bob - Original Clean Architecture explanation
- [CQRS Journey by Microsoft](https://docs.microsoft.com/en-us/previous-versions/msp-n-p/jj554200(v=pandp.10)) - Comprehensive
  CQRS implementation guide
- [Domain-Driven Design Reference](https://www.domainlanguage.com/ddd/reference/) by Eric Evans - DDD quick reference
- [Event Sourcing Pattern](https://martinfowler.com/eaaDev/EventSourcing.html) by Martin Fowler - Event sourcing fundamentals

### Sample Projects & Repositories

#### .NET Architecture Samples

- [eShopOnContainers](https://github.com/dotnet-architecture/eShopOnContainers) - Microsoft's reference microservices application
- [Clean Architecture Solution Template](https://github.com/jasontaylordev/CleanArchitecture) by Jason Taylor - Popular
  Clean Architecture template
- [NorthwindTraders](https://github.com/jasontaylordev/NorthwindTraders) - Clean Architecture with CQRS example
- [Modular Monolith](https://github.com/kgrzybek/modular-monolith-with-ddd) - DDD and modular monolith patterns

#### Testing Resources

- [TestContainers .NET](https://github.com/testcontainers/testcontainers-dotnet) - Integration testing with containers
- [SpecFlow](https://specflow.org/) - Behavior-driven development for .NET
- [Bogus](https://github.com/bchavez/Bogus) - Fake data generation for testing

### Courses & Learning Platforms

#### Online Courses

- **Pluralsight**: "Clean Architecture: Patterns, Practices, and Principles" by Matthew Renze
- **Udemy**: "Complete Guide to Building an App with .NET Core and React" by Neil Cummings
- **Microsoft Learn**: [.NET Learning Paths](https://docs.microsoft.com/en-us/learn/dotnet/) - Free Microsoft training
- **Coursera**: "Software Architecture" by University of Alberta

#### YouTube Channels

- **Derek Comartin** - Software architecture and .NET content
- **Nick Chapsas** - Advanced .NET programming techniques
- **Milan Jovanović** - Clean Architecture and DDD in .NET
- **IAmTimCorey** - C# and .NET best practices

### Blogs & Websites

#### Architecture & Design

- [Martin Fowler's Blog](https://martinfowler.com/) - Enterprise architecture patterns and practices
- [DDD Community](https://dddcommunity.org/) - Domain-driven design resources and community
- [Event Store Blog](https://www.eventstore.com/blog) - Event sourcing and CQRS insights
- [Udi Dahan's Blog](https://udidahan.com/) - Service-oriented architecture and messaging

#### .NET Development

- [.NET Blog](https://devblogs.microsoft.com/dotnet/) - Official Microsoft .NET updates
- [Code Maze](https://code-maze.com/) - .NET tutorials and best practices
- [Andrew Lock's Blog](https://andrewlock.net/) - ASP.NET Core deep dives
- [Steve Gordon's Blog](https://www.stevejgordon.co.uk/) - .NET performance and best practices

### Tools & Libraries

#### Development Tools

- **JetBrains Rider** or **Visual Studio** - Primary IDEs for .NET development
- **Docker Desktop** - Containerization for development and testing
- **Postman** or **Insomnia** - API testing and documentation
- **SQL Server Management Studio** - Database management

#### Useful NuGet Packages

- **MediatR** - CQRS and mediator pattern implementation
- **FluentValidation** - Input validation with fluent interface
- **AutoMapper** - Object-to-object mapping
- **Polly** - Resilience and transient-fault-handling
- **Serilog** - Structured logging framework
- **Swashbuckle** - OpenAPI/Swagger documentation generation

### Conferences & Events

#### Major .NET Conferences

- **.NET Conf** - Annual Microsoft .NET conference (free, online)
- **NDC Conferences** - Developer conferences with strong .NET track
- **DDD Europe** - Domain-driven design focused conference
- **DevIntersection** - Microsoft-focused developer conference

#### Local Meetups

- Search for local .NET user groups and meetups in your area
- Join online communities like .NET Foundation Discord or Reddit r/dotnet

---

*Remember: Start with the fundamentals and gradually work your way up to more*
*advanced topics. The key is consistent practice and applying these patterns in*
*real projects.*
