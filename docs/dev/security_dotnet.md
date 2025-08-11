# Security Policy

## Table of Contents

- [Supported Versions](#supported-versions)
- [Reporting a Vulnerability](#reporting-a-vulnerability)
- [.NET Security Best Practices](#net-security-best-practices)
  - [Authentication and Authorization](#authentication-and-authorization)
    - [ASP.NET Core Identity Configuration](#aspnet-core-identity-configuration)
    - [JWT Token Security](#jwt-token-security)
    - [Authorization Policies](#authorization-policies)
  - [Data Protection and Encryption](#data-protection-and-encryption)
    - [Sensitive Data Protection](#sensitive-data-protection)
    - [Configuration Data Protection](#configuration-data-protection)
  - [Input Validation and Sanitization](#input-validation-and-sanitization)
    - [Model Validation](#model-validation)
    - [Custom Validation Attributes](#custom-validation-attributes)
  - [SQL Injection Prevention](#sql-injection-prevention)
    - [Safe Entity Framework Usage](#safe-entity-framework-usage)
  - [Cross-Site Scripting (XSS) Prevention](#cross-site-scripting-xss-prevention)
    - [Output Encoding](#output-encoding)
    - [Content Security Policy](#content-security-policy)
  - [HTTPS and TLS Configuration](#https-and-tls-configuration)
  - [Secrets Management](#secrets-management)
    - [User Secrets (Development)](#user-secrets-development)
    - [Azure Key Vault (Production)](#azure-key-vault-production)
  - [Rate Limiting and DDoS Protection](#rate-limiting-and-ddos-protection)
  - [Logging and Monitoring](#logging-and-monitoring)
    - [Security Event Logging](#security-event-logging)
    - [Structured Logging Configuration](#structured-logging-configuration)
  - [Database Security](#database-security)
    - [Connection String Security](#connection-string-security)
    - [Data Encryption at Rest](#data-encryption-at-rest)
  - [Error Handling and Information Disclosure](#error-handling-and-information-disclosure)
- [Security Checklist](#security-checklist)
  - [Development Phase](#development-phase)
  - [Production Phase](#production-phase)
  - [Infrastructure](#infrastructure)
- [Security Tools and Libraries](#security-tools-and-libraries)
  - [Static Analysis](#static-analysis)
  - [Runtime Security](#runtime-security)
  - [Dependencies](#dependencies)
- [Security Training Resources](#security-training-resources)

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Reporting a Vulnerability

To report a security vulnerability, please:

1. **DO NOT** open a public GitHub issue

2. Email security contact with:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if any)

You can expect:

- Acknowledgment within 24 hours
- Status update within 72 hours
- Security advisory if needed

## .NET Security Best Practices

### Authentication and Authorization

#### ASP.NET Core Identity Configuration

```csharp
// Startup.cs or Program.cs
services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
```

#### JWT Token Security

```csharp
public class JwtService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;

    public string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add role claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"])),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
```

#### Authorization Policies

```csharp
// Program.cs
services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("RequireManagerOrAdmin", policy =>
        policy.RequireRole("Manager", "Admin"));
    
    options.AddPolicy("RequireEmailVerified", policy =>
        policy.RequireClaim("email_verified", "true"));
    
    options.AddPolicy("RequireMFA", policy =>
        policy.RequireClaim("mfa", "true"));
});

// Usage in controllers
[Authorize(Policy = "RequireAdminRole")]
public class AdminController : ControllerBase
{
    // Admin-only endpoints
}
```

### Data Protection and Encryption

#### Sensitive Data Protection

```csharp
public class UserService
{
    private readonly IDataProtector _protector;
    private readonly ApplicationDbContext _context;

    public UserService(IDataProtectionProvider dataProtectionProvider, ApplicationDbContext context)
    {
        _protector = dataProtectionProvider.CreateProtector("UserService.SensitiveData");
        _context = context;
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var user = new User
        {
            Email = request.Email,
            // Encrypt sensitive data
            SocialSecurityNumber = _protector.Protect(request.SocialSecurityNumber),
            PhoneNumber = _protector.Protect(request.PhoneNumber)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public string GetDecryptedSSN(User user)
    {
        return _protector.Unprotect(user.SocialSecurityNumber);
    }
}
```

#### Configuration Data Protection

```csharp
// Program.cs
services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"./keys/"))
    .SetApplicationName("YourAppName")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// For production with Azure Key Vault
services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(connectionString, containerName, blobName)
    .ProtectKeysWithAzureKeyVault(keyIdentifier, credential);
```

### Input Validation and Sanitization

#### Model Validation

```csharp
public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Name can only contain letters and spaces")]
    public string Name { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]", 
        ErrorMessage = "Password must contain uppercase, lowercase, digit, and special character")]
    public string Password { get; set; }
}
```

#### Custom Validation Attributes

```csharp
public class NoScriptAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is string stringValue && stringValue.Contains("<script", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult("Script tags are not allowed");
        }
        return ValidationResult.Success;
    }
}

public class SafeHtmlAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is string html)
        {
            var sanitizer = new HtmlSanitizer();
            var sanitized = sanitizer.Sanitize(html);
            
            if (html != sanitized)
            {
                return new ValidationResult("HTML content contains potentially dangerous elements");
            }
        }
        return ValidationResult.Success;
    }
}
```

### SQL Injection Prevention

#### Safe Entity Framework Usage

```csharp
public class UserRepository
{
    private readonly ApplicationDbContext _context;

    // GOOD: Parameterized query with EF Core
    public async Task<User> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Where(u => u.Email == email) // Automatically parameterized
            .FirstOrDefaultAsync();
    }

    // GOOD: Raw SQL with parameters
    public async Task<List<User>> GetUsersByStatusAsync(string status)
    {
        return await _context.Users
            .FromSqlRaw("SELECT * FROM Users WHERE Status = {0}", status)
            .ToListAsync();
    }

    // BAD: Never concatenate user input into SQL
    // public async Task<User> GetUserUnsafe(string email)
    // {
    //     return await _context.Users
    //         .FromSqlRaw($"SELECT * FROM Users WHERE Email = '{email}'") // VULNERABLE!
    //         .FirstOrDefaultAsync();
    // }
}
```

### Cross-Site Scripting (XSS) Prevention

#### Output Encoding

```csharp
// In Razor views - automatic HTML encoding
<div>@Model.UserInput</div> <!-- Automatically encoded -->

// For raw HTML (use carefully)
<div>@Html.Raw(Html.Encode(Model.UserInput))</div>

// In controllers returning JSON
public IActionResult GetUser(int id)
{
    var user = _userService.GetUser(id);
    return Json(user); // Automatically encoded
}
```

#### Content Security Policy

```csharp
// Middleware for CSP headers
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'");
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

        await _next(context);
    }
}

// Register in Program.cs
app.UseMiddleware<SecurityHeadersMiddleware>();
```

### HTTPS and TLS Configuration

```csharp
// Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHsts(); // HTTP Strict Transport Security
}

app.UseHttpsRedirection();

// Configure HTTPS in appsettings.json
{
  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "http://localhost:5000"
      },
      "Https": {
        "Url": "https://localhost:5001",
        "Certificate": {
          "Path": "certificate.pfx",
          "Password": "password"
        }
      }
    }
  }
}
```

### Secrets Management

#### User Secrets (Development)

```bash
# Initialize user secrets
dotnet user-secrets init

# Add secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=..."
dotnet user-secrets set "ApiKeys:ExternalService" "secret-key"
```

#### Azure Key Vault (Production)

```csharp
// Program.cs
if (app.Environment.IsProduction())
{
    var keyVaultURL = builder.Configuration["KeyVaultURL"];
    var credential = new DefaultAzureCredential();
    
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultURL), 
        credential);
}

// Usage
public class ExternalApiService
{
    private readonly string _apiKey;
    
    public ExternalApiService(IConfiguration configuration)
    {
        _apiKey = configuration["ApiKeys:ExternalService"];
    }
}
```

### Rate Limiting and DDoS Protection

```csharp
// Using AspNetCoreRateLimit
services.AddMemoryCache();
services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
services.AddInMemoryRateLimiting();
services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Configuration in appsettings.json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "RealIPHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      },
      {
        "Endpoint": "POST:/api/auth/login",
        "Period": "15m",
        "Limit": 5
      }
    ]
  }
}
```

### Logging and Monitoring

#### Security Event Logging

```csharp
public class SecurityAuditService
{
    private readonly ILogger<SecurityAuditService> _logger;

    public SecurityAuditService(ILogger<SecurityAuditService> logger)
    {
        _logger = logger;
    }

    public void LogSuccessfulLogin(string userId, string ipAddress)
    {
        _logger.LogInformation("User {UserId} successfully logged in from {IpAddress}", 
            userId, ipAddress);
    }

    public void LogFailedLogin(string email, string ipAddress, string reason)
    {
        _logger.LogWarning("Failed login attempt for {Email} from {IpAddress}. Reason: {Reason}", 
            email, ipAddress, reason);
    }

    public void LogPrivilegeEscalation(string userId, string action)
    {
        _logger.LogWarning("Potential privilege escalation: User {UserId} attempted {Action}", 
            userId, action);
    }

    public void LogSuspiciousActivity(string userId, string activity, string details)
    {
        _logger.LogError("Suspicious activity detected: User {UserId}, Activity: {Activity}, Details: {Details}", 
            userId, activity, details);
    }
}
```

#### Structured Logging Configuration

```csharp
// Program.cs
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces));
```

### Database Security

#### Connection String Security

```csharp
// appsettings.json (use environment variables or Key Vault in production)
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;Trusted_Connection=true;Encrypt=true;TrustServerCertificate=false;"
  }
}

// Always use encrypted connections
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure();
        sqlOptions.CommandTimeout(30);
    }));
```

#### Data Encryption at Rest

```csharp
// Entity Framework with Always Encrypted
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    
    [Column(TypeName = "varchar(50)")]
    [ColumnEncryption(EncryptionType.Deterministic, "CEK_Auto1")]
    public string SocialSecurityNumber { get; set; }
}
```

### Error Handling and Information Disclosure

```csharp
// Global exception handler
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = exception switch
        {
            NotFoundException => 404,
            UnauthorizedException => 401,
            ValidationException => 400,
            _ => 500
        };

        var response = new
        {
            error = exception switch
            {
                // Don't expose sensitive information
                NotFoundException => "Resource not found",
                UnauthorizedException => "Unauthorized access",
                ValidationException => "Invalid request data",
                _ => "An error occurred while processing your request"
            },
            // Only include details in development
            details = _environment.IsDevelopment() ? exception.Message : null
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

## Security Checklist

### Development Phase

- [ ] Input validation on all user inputs
- [ ] Output encoding to prevent XSS
- [ ] Parameterized queries to prevent SQL injection
- [ ] Proper authentication and authorization
- [ ] Secure password storage (hashed + salted)
- [ ] HTTPS everywhere
- [ ] Security headers configured
- [ ] Secrets stored securely (not in code)
- [ ] Error handling doesn't expose sensitive info
- [ ] Dependencies regularly updated

### Production Phase

- [ ] TLS/SSL certificates properly configured
- [ ] Rate limiting implemented
- [ ] Security monitoring and alerting
- [ ] Regular security updates applied
- [ ] Database connections encrypted
- [ ] Backup encryption enabled
- [ ] Access logging enabled
- [ ] Principle of least privilege applied
- [ ] Security headers tested
- [ ] Vulnerability scanning performed

### Infrastructure

- [ ] Network segmentation
- [ ] Firewall rules configured
- [ ] Database access restricted
- [ ] Monitoring and alerting configured
- [ ] Incident response plan documented
- [ ] Regular security audits scheduled
- [ ] Backup and recovery tested
- [ ] Access controls reviewed

## Security Tools and Libraries

### Static Analysis

- **SonarQube**: Code quality and security analysis
- **CodeQL**: Semantic code analysis
- **Security Code Scan**: Security-focused analyzer for .NET
- **DevSkim**: Security linter

### Runtime Security

- **OWASP ZAP**: Web application security scanner
- **Burp Suite**: Web vulnerability scanner
- **ASP.NET Core Data Protection**: Built-in data protection
- **IdentityServer**: OpenID Connect and OAuth 2.0 framework

### Dependencies

- **OWASP Dependency Check**: Vulnerability scanning for dependencies
- **Snyk**: Dependency vulnerability monitoring
- **GitHub Security Advisories**: Automatic vulnerability alerts
- **NuGet Audit**: Built-in package vulnerability scanning

## Security Training Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Microsoft Security Development Lifecycle](https://www.microsoft.com/en-us/securityengineering/sdl)
- [ASP.NET Core Security Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [Azure Security Best Practices](https://docs.microsoft.com/en-us/azure/security/fundamentals/best-practices-and-patterns)
