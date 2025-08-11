# 🔄 GitHub Actions CI/CD Best Practices for .NET

## Table of Contents

- [🔄 GitHub Actions CI/CD Best Practices for .NET](#-github-actions-cicd-best-practices-for-net)
  - [Table of Contents](#table-of-contents)
  - [Workflow Structure](#workflow-structure)
    - [Basic .NET Workflow Anatomy](#basic-net-workflow-anatomy)
    - [Key Components for .NET Workflows](#key-components-for-net-workflows)
  - [.NET-Specific Best Practices](#net-specific-best-practices)
    - [1. .NET Workflow Design](#1-net-workflow-design)
    - [2. .NET Performance Optimization](#2-net-performance-optimization)
    - [3. .NET Security Best Practices](#3-net-security-best-practices)
    - [4. .NET Workflow Management](#4-net-workflow-management)
  - [.NET Code Quality and Testing](#net-code-quality-and-testing)
    - [Testing Strategies](#testing-strategies)
    - [Code Quality Tools](#code-quality-tools)
  - [.NET Deployment Patterns](#net-deployment-patterns)
    - [Container Deployment](#container-deployment)
    - [Azure Deployment](#azure-deployment)
    - [NuGet Package Publishing](#nuget-package-publishing)
  - [Advanced .NET Patterns](#advanced-net-patterns)
    - [1. Multi-Project Solution Workflows](#1-multi-project-solution-workflows)
    - [2. Custom .NET Actions](#2-custom-net-actions)
    - [3. Database Migration Workflows](#3-database-migration-workflows)
  - [Security Hardening for .NET GitHub Actions](#security-hardening-for-net-github-actions)
    - [.NET-Specific Security Considerations](#net-specific-security-considerations)
    - [Package Security](#package-security)
    - [Configuration Security](#configuration-security)
    - [Container Security for .NET](#container-security-for-net)
    - [Code Analysis and Security Rules](#code-analysis-and-security-rules)
  - [🚨 .NET Security Checklist for GitHub Actions](#-net-security-checklist-for-github-actions)
  - [📚 Additional .NET Resources](#-additional-net-resources)
  - [References](#references)
  - [💡 Implementation Tips for .NET Teams](#-implementation-tips-for-net-teams)
    - [Getting Started](#getting-started)
    - [Common Pitfalls](#common-pitfalls)
    - [Team Adoption](#team-adoption)

> *Last updated: June 2025*

This document provides concise guidance for implementing effective CI/CD workflows
with GitHub Actions specifically for .NET applications, focusing on modern .NET 9+
development practices.

<!-- REF: https://docs.github.com/en/actions -->
<!-- REF: https://docs.microsoft.com/en-us/dotnet/devops/ -->
<!-- REF: https://github.com/actions/setup-dotnet -->

## Workflow Structure

### Basic .NET Workflow Anatomy

```yaml
name: .NET CI Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

env:
  DOTNET_VERSION: '8.0.x'
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: 'true'
  DOTNET_NOLOGO: 'true'
  DOTNET_CLI_TELEMETRY_OPTOUT: 'true'

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@692973e3d937129bcbf40652eb9f2f61becf3332 # v4.1.7
      
      - name: Setup .NET
        uses: actions/setup-dotnet@4d6c8fcf3c8f7a60068d26b594648e99df24cee3 # v4.0.0
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}
          
      - name: Restore dependencies
        run: dotnet restore
        
      - name: Build
        run: dotnet build --no-restore --configuration Release
        
      - name: Test
        run: dotnet test --no-build --configuration Release --verbosity normal --collect:"XPlat Code Coverage"
        
      - name: Upload coverage reports
        uses: codecov/codecov-action@e28ff129e5465c2c0dcc6f003fc735cb6ae0c673 # v4.5.0
        with:
          files: '**/coverage.cobertura.xml'
```

### Key Components for .NET Workflows

- **Workflow file**: YAML files in `.github/workflows/`
- **Triggers** (`on`): Events that start the workflow (push, PR, release)
- **Jobs**: Groups of steps (build, test, deploy, security-scan)
- **Steps**: Individual tasks (restore, build, test, publish)
- **Actions**: Reusable components (`actions/setup-dotnet`, `actions/checkout`)
- **Runners**: VMs (ubuntu-latest for cross-platform, windows-latest for Windows-specific)

## .NET-Specific Best Practices

### 1. .NET Workflow Design

- **Multi-project solutions**: Handle complex solution structures efficiently
- **Framework targeting**: Support multiple .NET versions when needed
- **Package management**: Leverage NuGet package caching
- **Configuration management**: Use proper environment-based configurations

```yaml
# Good practice: Multi-framework and caching
name: .NET Multi-Target Build

on:
  push:
    branches: [ main ]
    paths:
      - 'src/**'
      - '**/*.csproj'
      - '**/*.sln'
  pull_request:
    branches: [ main ]
    paths-ignore:
      - '**/*.md'
      - 'docs/**'

env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: 'true'
  DOTNET_NOLOGO: 'true'
  DOTNET_CLI_TELEMETRY_OPTOUT: 'true'

jobs:
  build:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        dotnet-version: ['6.0.x', '8.0.x']
        
    steps:
      - uses: actions/checkout@692973e3d937129bcbf40652eb9f2f61becf3332 # v4.1.7
      
      - name: Setup .NET ${{ matrix.dotnet-version }}
        uses: actions/setup-dotnet@4d6c8fcf3c8f7a60068d26b594648e99df24cee3 # v4.0.0
        with:
          dotnet-version: ${{ matrix.dotnet-version }}
          
      - name: Cache NuGet packages
        uses: actions/cache@0c45773b623bea8c8e75f6c82b208c3cf94ea4f9 # v4.0.2
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/packages.lock.json') }}
          restore-keys: |
            ${{ runner.os }}-nuget-
```

### 2. .NET Performance Optimization

- **NuGet package caching**: Cache packages across workflow runs
- **Build output caching**: Cache build artifacts when appropriate
- **Parallel test execution**: Leverage xUnit parallel execution
- **Conditional builds**: Skip rebuilds when only docs change

```yaml
# Example: Advanced .NET caching and optimization
steps:
  - uses: actions/checkout@692973e3d937129bcbf40652eb9f2f61becf3332 # v4.1.7
  
  - name: Cache NuGet packages
    uses: actions/cache@0c45773b623bea8c8e75f6c82b208c3cf94ea4f9 # v4.0.2
    with:
      path: ~/.nuget/packages
      key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/packages.lock.json') }}
      restore-keys: ${{ runner.os }}-nuget-
      
  - name: Restore dependencies
    run: dotnet restore --locked-mode
    
  - name: Build solution
    run: dotnet build --no-restore --configuration Release
    
  - name: Run tests with coverage
    run: |
      dotnet test --no-build --configuration Release \
        --collect:"XPlat Code Coverage" \
        --results-directory ./coverage \
        --logger trx \
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

# Example: Matrix builds for multiple OS and .NET versions
jobs:
  test:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
        dotnet-version: ['6.0.x', '8.0.x']
        exclude:
          - os: macos-latest
            dotnet-version: '6.0.x'  # Skip older versions on macOS
```

### 3. .NET Security Best Practices

- **NuGet package vulnerability scanning**: Use `dotnet list package --vulnerable`
- **Code analysis**: Integrate Roslyn analyzers and security rules
- **Secrets management**: Use ASP.NET Core user secrets and Key Vault
- **Container security**: Scan Docker images for .NET applications

```yaml
# Example: .NET security scanning
jobs:
  security-scan:
    runs-on: ubuntu-latest
    permissions:
      security-events: write
      contents: read
      
    steps:
      - uses: actions/checkout@692973e3d937129bcbf40652eb9f2f61becf3332 # v4.1.7
      
      - name: Setup .NET
        uses: actions/setup-dotnet@4d6c8fcf3c8f7a60068d26b594648e99df24cee3 # v4.0.0
        with:
          dotnet-version: '8.0.x'
          
      - name: Restore dependencies
        run: dotnet restore
        
      - name: Check for vulnerable packages
        run: dotnet list package --vulnerable --include-transitive
        
      - name: Run .NET Code Analysis
        run: dotnet build --configuration Release --verbosity normal /p:RunAnalyzersDuringBuild=true
        
      - name: Initialize CodeQL
        uses: github/codeql-action/init@b611370bb5703a7efb587f9d136a52ea24c5c38c # v3.25.11
        with:
          languages: csharp
          
      - name: Perform CodeQL Analysis
        uses: github/codeql-action/analyze@b611370bb5703a7efb587f9d136a52ea24c5c38c # v3.25.11
```

### 4. .NET Workflow Management

- **Solution-based organization**: Organize workflows around solution structure
- **Environment-specific configurations**: Use `appsettings.{Environment}.json` patterns
- **MSBuild properties**: Leverage MSBuild for conditional compilation
- **Package versioning**: Implement semantic versioning for NuGet packages

```yaml
# Example: Environment-specific deployment
jobs:
  deploy-to-staging:
    if: github.ref == 'refs/heads/develop'
    runs-on: ubuntu-latest
    environment: staging
    timeout-minutes: 30
    
    steps:
      - uses: actions/checkout@692973e3d937129bcbf40652eb9f2f61becf3332 # v4.1.7
      
      - name: Setup .NET
        uses: actions/setup-dotnet@4d6c8fcf3c8f7a60068d26b594648e99df24cee3 # v4.0.0
        with:
          dotnet-version: '8.0.x'
          
      - name: Publish application
        run: |
          dotnet publish src/MyApp/MyApp.csproj \
            --configuration Release \
            --output ./publish \
            /p:PublishProfile=FolderProfile \
            /p:EnvironmentName=Staging
```

## .NET Code Quality and Testing

### Testing Strategies

- **Unit testing**: xUnit, NUnit, or MSTest with proper isolation
- **Integration testing**: ASP.NET Core TestServer for web APIs
- **End-to-end testing**: Playwright for web applications
- **Performance testing**: NBomber or custom benchmarks

```yaml
# Example: Comprehensive .NET testing pipeline
jobs:
  test:
    runs-on: ubuntu-latest
    services:
      sql-server:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: YourStrong@Passw0rd
          ACCEPT_EULA: Y
        ports:
          - 1433:1433
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q 'SELECT 1'"
          --health-interval 10s
          --health-timeout 3s
          --health-retries 10
          
    steps:
      - uses: actions/checkout@692973e3d937129bcbf40652eb9f2f61becf3332 # v4.1.7
      
      - name: Setup .NET
        uses: actions/setup-dotnet@4d6c8fcf3c8f7a60068d26b594648e99df24cee3 # v4.0.0
        with:
          dotnet-version: '8.0.x'
          
      - name: Restore dependencies
        run: dotnet restore
        
      - name: Build
        run: dotnet build --no-restore --configuration Release
        
      - name: Run unit tests
        run: |
          dotnet test tests/UnitTests/ \
            --no-build --configuration Release \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage/unit
            
      - name: Run integration tests
        env:
          ConnectionStrings__DefaultConnection: "Server=localhost,1433;Database=TestDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true"
        run: |
          dotnet test tests/IntegrationTests/ \
            --no-build --configuration Release \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage/integration
            
      - name: Generate coverage report
        uses: danielpalme/ReportGenerator-GitHub-Action@09b9b2e2b3b35324e3f3129bf21c4df3c1e6ca1d # 5.3.8
        with:
          reports: 'coverage/**/coverage.cobertura.xml'
          targetdir: 'coverage/report'
          reporttypes: 'HtmlInline;Cobertura;SonarQube'
          
      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@e28ff129e5465c2c0dcc6f003fc735cb6ae0c673 # v4.5.0
        with:
          files: './coverage/report/Cobertura.xml'
          fail_ci_if_error: true
```

### Code Quality Tools

```yaml
# Example: .NET code quality and static analysis
steps:
  - name: Run StyleCop analysis
    run: dotnet build --configuration Release /p:RunStyleCopAnalysis=true
    
  - name: Run SonarCloud analysis
    env:
      GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
    run: |
      dotnet tool install --global dotnet-sonarscanner
      dotnet sonarscanner begin /k:"your-project-key" /o:"your-org" /d:sonar.login="${{ secrets.SONAR_TOKEN }}" /d:sonar.host.url="https://sonarcloud.io"
      dotnet build --configuration Release
      dotnet sonarscanner end /d:sonar.login="${{ secrets.SONAR_TOKEN }}"
```

## .NET Deployment Patterns

### Container Deployment

```yaml
# Example: Docker build and deployment for .NET
jobs:
  docker-build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@692973e3d937129bcbf40652eb9f2f61becf3332 # v4.1.7
      
      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@d70bba72b1f3fd22344832f00baa16ece964efeb # v3.3.0
        
      - name: Login to Container Registry
        uses: docker/login-action@0d4c9c5ea7693da7b068278f7b52bda2a190a446 # v3.2.0
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
          
      - name: Build and push Docker image
        uses: docker/build-push-action@15560696de535e4014efeff63c48f16952e52dd1 # v6.2.0
        with:
          context: .
          file: ./Dockerfile
          push: true
          tags: |
            ghcr.io/${{ github.repository }}:latest
            ghcr.io/${{ github.repository }}:${{ github.sha }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

### Azure Deployment

```yaml
# Example: Azure App Service deployment
jobs:
  deploy-to-azure:
    runs-on: ubuntu-latest
    environment: production
    permissions:
      id-token: write
      contents: read
      
    steps:
      - uses: actions/checkout@692973e3d937129bcbf40652eb9f2f61becf3332 # v4.1.7
      
      - name: Setup .NET
        uses: actions/setup-dotnet@4d6c8fcf3c8f7a60068d26b594648e99df24cee3 # v4.0.0
        with:
          dotnet-version: '8.0.x'
          
      - name: Azure Login with OIDC
        uses: azure/login@6c251865b4e6290e7b78be643ea2d005bc51f69a # v2.1.1
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
          
      - name: Build and publish
        run: |
          dotnet publish src/WebApp/WebApp.csproj \
            --configuration Release \
            --output ./publish \
            /p:PublishProfile=Azure
            
      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@de617f46172a906d0617bb0e50d81e9e3aec24c6 # v3.0.1
        with:
          app-name: ${{ secrets.AZURE_APP_NAME }}
          package: ./publish
```

### NuGet Package Publishing

```yaml
# Example: NuGet package creation and publishing
jobs:
  publish-nuget:
    if: startsWith(github.ref, 'refs/tags/v')
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@692973e3d937129bcbf40652eb9f2f61becf3332 # v4.1.7
      
      - name: Setup .NET
        uses: actions/setup-dotnet@4d6c8fcf3c8f7a60068d26b594648e99df24cee3 # v4.0.0
        with:
          dotnet-version: '8.0.x'
          
      - name: Extract version from tag
        id: version
        run: echo "VERSION=${GITHUB_REF#refs/tags/v}" >> $GITHUB_OUTPUT
        
      - name: Restore dependencies
        run: dotnet restore
        
      - name: Build
        run: dotnet build --configuration Release --no-restore
        
      - name: Pack NuGet packages
        run: |
          dotnet pack src/MyLibrary/MyLibrary.csproj \
            --configuration Release \
            --no-build \
            --output ./packages \
            /p:PackageVersion=${{ steps.version.outputs.VERSION }}
            
      - name: Publish to NuGet.org
        run: |
          dotnet nuget push ./packages/*.nupkg \
            --source https://api.nuget.org/v3/index.json \
            --api-key ${{ secrets.NUGET_API_KEY }} \
            --skip-duplicate
```

## Advanced .NET Patterns

### 1. Multi-Project Solution Workflows

```yaml
# Reusable workflow for .NET solutions
name: .NET Solution Build
on:
  workflow_call:
    inputs:
      solution-path:
        required: true
        type: string
      dotnet-version:
        required: false
        type: string
        default: '8.0.x'
      run-tests:
        required: false
        type: boolean
        default: true

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@692973e3d937129bcbf40652eb9f2f61becf3332 # v4.1.7
      
      - name: Setup .NET
        uses: actions/setup-dotnet@4d6c8fcf3c8f7a60068d26b594648e99df24cee3 # v4.0.0
        with:
          dotnet-version: ${{ inputs.dotnet-version }}
          
      - name: Restore solution
        run: dotnet restore ${{ inputs.solution-path }}
        
      - name: Build solution
        run: dotnet build ${{ inputs.solution-path }} --no-restore --configuration Release
        
      - name: Test solution
        if: ${{ inputs.run-tests }}
        run: dotnet test ${{ inputs.solution-path }} --no-build --configuration Release
```

### 2. Custom .NET Actions

```yaml
# .github/actions/dotnet-setup/action.yml
name: 'Setup .NET with caching'
description: 'Setup .NET SDK with NuGet package caching'
inputs:
  dotnet-version:
    description: '.NET version to use'
    required: false
    default: '8.0.x'
    
runs:
  using: 'composite'
  steps:
    - name: Setup .NET
      uses: actions/setup-dotnet@4d6c8fcf3c8f7a60068d26b594648e99df24cee3 # v4.0.0
      with:
        dotnet-version: ${{ inputs.dotnet-version }}
        
    - name: Cache NuGet packages
      uses: actions/cache@0c45773b623bea8c8e75f6c82b208c3cf94ea4f9 # v4.0.2
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/packages.lock.json') }}
        restore-keys: ${{ runner.os }}-nuget-
      shell: bash
```

### 3. Database Migration Workflows

```yaml
# Example: Entity Framework migrations
jobs:
  migrate-database:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@692973e3d937129bcbf40652eb9f2f61becf3332 # v4.1.7
      
      - name: Setup .NET
        uses: actions/setup-dotnet@4d6c8fcf3c8f7a60068d26b594648e99df24cee3 # v4.0.0
        with:
          dotnet-version: '8.0.x'
          
      - name: Install EF CLI tools
        run: dotnet tool install --global dotnet-ef
        
      - name: Run database migrations
        env:
          ConnectionStrings__DefaultConnection: ${{ secrets.DATABASE_CONNECTION_STRING }}
        run: |
          dotnet ef database update \
            --project src/Infrastructure \
            --startup-project src/WebApi \
            --configuration Release
```

## Security Hardening for .NET GitHub Actions

### .NET-Specific Security Considerations

1. **NuGet Package Security**: Verify package integrity and scan for vulnerabilities
2. **Configuration Security**: Protect connection strings and API keys
3. **Code Analysis**: Use .NET analyzers and security rules
4. **Container Security**: Secure .NET container images

### Package Security

```yaml
# Example: NuGet package security scanning
steps:
  - name: Audit NuGet packages
    run: |
      # Check for vulnerable packages
      dotnet list package --vulnerable --include-transitive
      
      # Verify package integrity (if using package signing)
      dotnet nuget verify **/*.nupkg
      
      # Check for deprecated packages
      dotnet list package --deprecated
```

### Configuration Security

```yaml
# Example: Secure configuration management
jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - name: Deploy with secure configuration
        env:
          # Use environment-specific secrets
          ConnectionStrings__DefaultConnection: ${{ secrets.PROD_DATABASE_CONNECTION }}
          Authentication__JwtSecret: ${{ secrets.JWT_SECRET }}
          ExternalServices__ApiKey: ${{ secrets.EXTERNAL_API_KEY }}
        run: |
          # Deploy with environment variables
          dotnet publish --configuration Release \
            /p:EnvironmentName=Production \
            /p:UseSecureConfiguration=true
```

### Container Security for .NET

```yaml
# Example: Secure .NET container build
steps:
  - name: Build secure .NET container
    uses: docker/build-push-action@15560696de535e4014efeff63c48f16952e52dd1 # v6.2.0
    with:
      context: .
      file: ./Dockerfile
      push: true
      tags: myapp:${{ github.sha }}
      build-args: |
        BUILDKIT_INLINE_CACHE=1
        DOTNET_RUNNING_IN_CONTAINER=true
        ASPNETCORE_ENVIRONMENT=Production
        
  - name: Scan container for vulnerabilities
    uses: anchore/scan-action@3343887d815d7b07465f6fdcd395bd66508d486a # v3.6.4
    with:
      image: myapp:${{ github.sha }}
      fail-build: true
      severity-cutoff: high
```

### Code Analysis and Security Rules

```yaml
# Example: .NET security analysis
steps:
  - name: Run security analysis
    run: |
      # Enable security analyzers
      dotnet build --configuration Release \
        /p:RunAnalyzersDuringBuild=true \
        /p:TreatWarningsAsErrors=true \
        /p:WarningsAsErrors="" \
        /p:WarningsNotAsErrors="CS1591"
        
      # Run Roslyn security analyzers
      dotnet run --project tools/SecurityAnalyzer -- --solution MySolution.sln
```

## 🚨 .NET Security Checklist for GitHub Actions

- [ ] **.NET and NuGet Security**
  - [ ] NuGet packages are scanned for vulnerabilities with `dotnet list package --vulnerable`
  - [ ] Package restore uses locked mode (`--locked-mode`) when possible
  - [ ] Deprecated and outdated packages are identified and updated
  - [ ] Private NuGet feeds are secured with proper authentication

- [ ] **Code Analysis and Quality**
  - [ ] Roslyn analyzers are enabled with security rules
  - [ ] StyleCop and other code quality tools are integrated
  - [ ] Static analysis tools (SonarQube, CodeQL) are configured for C#
  - [ ] Build treats warnings as errors for critical security issues

- [ ] **Configuration and Secrets**
  - [ ] Connection strings and API keys use GitHub Secrets
  - [ ] Environment-specific configurations are properly managed
  - [ ] ASP.NET Core user secrets are not committed to repository
  - [ ] Azure Key Vault or similar is used for production secrets

- [ ] **Container and Deployment Security**
  - [ ] .NET container images use minimal base images (alpine, distroless)
  - [ ] Container images are scanned for vulnerabilities
  - [ ] Multi-stage Docker builds remove development dependencies
  - [ ] Runtime environments have security hardening applied

- [ ] **Testing and Validation**
  - [ ] Security-focused unit and integration tests are included
  - [ ] Authorization and authentication tests are comprehensive
  - [ ] Database migrations are tested in isolated environments
  - [ ] End-to-end security testing is automated

## 📚 Additional .NET Resources

- [.NET DevOps Documentation](https://docs.microsoft.com/en-us/dotnet/devops/)
- [GitHub Actions for .NET](https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net)
- [.NET Application Security](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [ASP.NET Core Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Entity Framework Security](https://docs.microsoft.com/en-us/ef/core/miscellaneous/security)
- [Azure DevOps for .NET](https://docs.microsoft.com/en-us/azure/devops/pipelines/ecosystems/dotnet-core)
- [Docker Best Practices for .NET](https://docs.microsoft.com/en-us/dotnet/core/docker/build-container)
- [.NET Analyzers and Code Quality](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)

## References

1. [GitHub Actions Documentation](https://docs.github.com/en/actions)
2. [Microsoft .NET DevOps Guide](https://docs.microsoft.com/en-us/dotnet/devops/)
3. [.NET Security Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/security/)
4. [GitHub Actions for .NET](https://github.com/actions/setup-dotnet)
5. [Azure DevOps for .NET Applications](https://docs.microsoft.com/en-us/azure/devops/pipelines/ecosystems/dotnet-core)
6. [Container Security for .NET](https://docs.microsoft.com/en-us/dotnet/core/docker/security)
7. [OWASP .NET Security](https://owasp.org/www-project-top-ten/)
8. [Microsoft Security Development Lifecycle](https://www.microsoft.com/en-us/securityengineering/sdl/)

---

## 💡 Implementation Tips for .NET Teams

### Getting Started

1. **Start with the basic workflow** and gradually add complexity
2. **Use matrix builds** to test across multiple .NET versions
3. **Implement caching** early to improve build performance
4. **Add security scanning** as a standard part of your pipeline
5. **Test your workflows** in a development environment first

### Common Pitfalls

- **Forgetting to cache NuGet packages** - significantly slows down builds
- **Not handling multi-project solutions** properly - can cause build failures
- **Ignoring security vulnerabilities** in dependencies
- **Not testing database migrations** in CI/CD pipeline
- **Hardcoding configuration values** instead of using environment variables

### Team Adoption

- **Document your workflows** and make them accessible to all team members
- **Provide training** on GitHub Actions and .NET DevOps practices
- **Establish code review processes** for workflow changes
- **Monitor and optimize** workflow performance regularly
- **Share knowledge** about best practices and lessons learned
