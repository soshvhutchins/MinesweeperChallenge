# Phase 2: Web API Integration Plan

## Overview

Phase 2 focuses on integrating the established Entity Framework infrastructure with the Web API layer, implementing authentication, real-time features, and comprehensive API documentation. This phase transforms our solid domain foundation into a fully functional web application.

## Phase 1 Achievements ✅

- **Core Infrastructure**: Entity Framework with SQLite/PostgreSQL support
- **Domain Logic**: Rich game models with comprehensive business rules
- **Data Persistence**: Repository pattern with EF Core implementation
- **CQRS Pattern**: MediatR-based command and query handlers
- **Database Schema**: Complete with Games, Players, and statistics tracking
- **Unit Testing**: 23 passing tests covering domain logic
- **Clean Architecture**: Proper dependency inversion between layers

## Phase 2 Objectives

### 🎯 Primary Goals

1. **API Integration**: Replace in-memory storage with repository-based persistence
2. **Authentication System**: JWT-based authentication with player registration/login
3. **Real-time Features**: SignalR hubs for live game updates and multiplayer support
4. **API Documentation**: Complete OpenAPI/Swagger documentation
5. **Security Hardening**: Input validation, rate limiting, and anti-cheat measures
6. **Integration Testing**: End-to-end API testing with TestContainers

## Implementation Roadmap

### 📋 Task 1: Controller Integration (Priority: High)

**Estimated Time**: 2-3 hours

#### Current State

- Controllers use in-memory game storage
- Basic CRUD operations without persistence
- No authentication or authorization

#### Target State

- Repository-based data access through MediatR
- Proper error handling and validation
- Security-first controller design

#### Controller Implementation Steps

1. **Update GamesController**

   ```csharp
   [Authorize]
   [ApiController]
   [Route("api/[controller]")]
   public class GamesController : ControllerBase
   {
       private readonly IMediator _mediator;

       [HttpPost]
       public async Task<ActionResult<GameDto>> CreateGame(
           [FromBody] CreateGameRequest request,
           [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
       {
           var playerId = GetCurrentPlayerId();
           var command = new StartNewGameCommand(playerId, request.Difficulty, idempotencyKey);
           var result = await _mediator.Send(command);
           
           return result.IsSuccess 
               ? CreatedAtAction(nameof(GetGame), new { id = result.Value }, result.Value)
               : BadRequest(new { error = result.Error });
       }
   }
   ```

2. **Implement Player Authentication Controller**

   ```csharp
   [ApiController]
   [Route("api/[controller]")]
   public class AuthController : ControllerBase
   {
       [HttpPost("register")]
       public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
       
       [HttpPost("login")]
       public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
       
       [HttpPost("refresh")]
       public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
   }
   ```

3. **Add Statistics Controller**

   ```csharp
   [Authorize]
   [ApiController]
   [Route("api/[controller]")]
   public class StatisticsController : ControllerBase
   {
       [HttpGet]
       public async Task<ActionResult<PlayerStatisticsDto>> GetPlayerStatistics()
       
       [HttpGet("leaderboard")]
       public async Task<ActionResult<LeaderboardDto>> GetLeaderboard([FromQuery] LeaderboardQuery query)
   }
   ```

#### Files to Modify

- `src/Minesweeper.WebApi/Controllers/GamesController.cs`
- `src/Minesweeper.WebApi/Controllers/AuthController.cs` (new)
- `src/Minesweeper.WebApi/Controllers/StatisticsController.cs` (new)
- `src/Minesweeper.Application/Commands/` (authentication commands)
- `src/Minesweeper.Application/Queries/` (statistics queries)

### 📋 Task 2: Authentication System (Priority: High)

**Estimated Time**: 3-4 hours

#### Authentication Implementation Steps

1. **JWT Configuration**

   ```json
   {
     "JwtSettings": {
       "SecretKey": "your-super-secret-key-here-at-least-32-characters",
       "Issuer": "MinesweeperAPI",
       "Audience": "MinesweeperClient",
       "ExpirationMinutes": 60,
       "RefreshTokenExpirationDays": 7
     }
   }
   ```

2. **Authentication Services**

   ```csharp
   public interface IAuthenticationService
   {
       Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);
       Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
       Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken);
       Task<Result> RevokeTokenAsync(string refreshToken);
   }
   ```

3. **Password Security**

   ```csharp
   public interface IPasswordService
   {
       string HashPassword(string password);
       bool VerifyPassword(string password, string hash);
       bool IsPasswordStrong(string password);
   }
   ```

#### Security Features

- **Password Hashing**: BCrypt with salt rounds
- **JWT Tokens**: Short-lived access tokens + longer refresh tokens
- **Rate Limiting**: Login attempt throttling
- **Input Validation**: FluentValidation for all authentication requests

#### Files to Create

- `src/Minesweeper.Application/Services/IAuthenticationService.cs`
- `src/Minesweeper.Application/Services/IPasswordService.cs`
- `src/Minesweeper.Infrastructure/Services/AuthenticationService.cs`
- `src/Minesweeper.Infrastructure/Services/PasswordService.cs`
- `src/Minesweeper.Application/Commands/Auth/` (register, login, refresh)

### 📋 Task 3: Real-time Features with SignalR (Priority: Medium)

**Estimated Time**: 2-3 hours

#### SignalR Implementation Steps

1. **Game Hub Implementation**

   ```csharp
   [Authorize]
   public class GameHub : Hub
   {
       public async Task JoinGame(string gameId)
       public async Task LeaveGame(string gameId)
       public async Task SendMove(string gameId, CellPosition position)
       public async Task SendChatMessage(string gameId, string message)
   }
   ```

2. **Real-time Events**
   - Game state updates (cell reveals, flags)
   - Player join/leave notifications
   - Game completion events
   - Chat messages (for multiplayer modes)

3. **Client Notifications**

   ```csharp
   public interface IGameClient
   {
       Task GameStateUpdated(GameStateDto gameState);
       Task PlayerJoined(string playerId, string username);
       Task PlayerLeft(string playerId);
       Task GameCompleted(GameCompletedDto result);
       Task ChatMessageReceived(ChatMessageDto message);
   }
   ```

#### Files to Create

- `src/Minesweeper.WebApi/Hubs/GameHub.cs`
- `src/Minesweeper.WebApi/Hubs/IGameClient.cs`
- `src/Minesweeper.Application/Events/` (SignalR event handlers)

### 📋 Task 4: Input Validation & Error Handling (Priority: High)

**Estimated Time**: 2-3 hours

#### Validation Implementation Steps

1. **FluentValidation Rules**

   ```csharp
   public class CreateGameValidator : AbstractValidator<CreateGameRequest>
   {
       public CreateGameValidator()
       {
           RuleFor(x => x.Difficulty)
               .NotNull()
               .Must(BeValidDifficulty);
               
           RuleFor(x => x.IdempotencyKey)
               .MaximumLength(100)
               .When(x => !string.IsNullOrEmpty(x.IdempotencyKey));
       }
   }
   ```

2. **Global Exception Handler**

   ```csharp
   public class GlobalExceptionMiddleware
   {
       public async Task InvokeAsync(HttpContext context, RequestDelegate next)
       {
           try { await next(context); }
           catch (Exception ex) { await HandleExceptionAsync(context, ex); }
       }
   }
   ```

3. **Standardized Error Responses**

   ```csharp
   public class ApiErrorResponse
   {
       public string Type { get; set; }
       public string Title { get; set; }
       public int Status { get; set; }
       public string Detail { get; set; }
       public Dictionary<string, string[]> Errors { get; set; }
   }
   ```

#### Files to Create

- `src/Minesweeper.Application/Validators/` (all request validators)
- `src/Minesweeper.WebApi/Middleware/GlobalExceptionMiddleware.cs`
- `src/Minesweeper.WebApi/Models/ApiErrorResponse.cs`

### 📋 Task 5: API Documentation & Testing (Priority: Medium)

**Estimated Time**: 1-2 hours

#### Documentation Implementation Steps

1. **Swagger Configuration**

   ```csharp
   builder.Services.AddSwaggerGen(c =>
   {
       c.SwaggerDoc("v1", new OpenApiInfo { Title = "Minesweeper API", Version = "v1" });
       c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { ... });
       c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Minesweeper.WebApi.xml"));
   });
   ```

2. **API Examples & Documentation**

   ```csharp
   /// <summary>
   /// Creates a new minesweeper game for the authenticated player
   /// </summary>
   /// <param name="request">Game creation parameters</param>
   /// <param name="idempotencyKey">Optional idempotency key for duplicate prevention</param>
   /// <returns>The created game information</returns>
   [HttpPost]
   [ProducesResponseType(typeof(GameDto), StatusCodes.Status201Created)]
   [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
   public async Task<ActionResult<GameDto>> CreateGame(...)
   ```

3. **Integration Tests**

   ```csharp
   public class GamesControllerTests : IClassFixture<WebApplicationFactory<Program>>
   {
       [Fact]
       public async Task CreateGame_WithValidRequest_ReturnsCreatedGame()
       
       [Fact]
       public async Task CreateGame_WithoutAuthentication_ReturnsUnauthorized()
   }
   ```

#### Files to Create

- `tests/Minesweeper.IntegrationTests/Controllers/` (API integration tests)
- `tests/Minesweeper.IntegrationTests/TestWebApplicationFactory.cs`

### 📋 Task 6: Security Hardening (Priority: High)

**Estimated Time**: 2-3 hours

#### Security Implementation Steps

1. **Rate Limiting**

   ```csharp
   builder.Services.AddRateLimiter(options =>
   {
       options.AddFixedWindowLimiter("AuthPolicy", limiterOptions =>
       {
           limiterOptions.Window = TimeSpan.FromMinutes(1);
           limiterOptions.PermitLimit = 5; // 5 login attempts per minute
       });
   });
   ```

2. **Anti-Cheat Measures**

   ```csharp
   public class AntiCheatService : IAntiCheatService
   {
       public Task<bool> ValidateMoveTimingAsync(GameId gameId, TimeSpan moveTime)
       public Task<bool> DetectSuspiciousPatternAsync(GameId gameId, IEnumerable<GameMove> moves)
       public Task LogSuspiciousActivityAsync(PlayerId playerId, string reason)
   }
   ```

3. **Input Sanitization**
   - HTML encoding for all text inputs
   - SQL injection prevention (handled by EF Core)
   - XSS protection headers
   - CORS configuration

#### Files to Create

- `src/Minesweeper.Application/Services/IAntiCheatService.cs`
- `src/Minesweeper.Infrastructure/Services/AntiCheatService.cs`
- `src/Minesweeper.WebApi/Middleware/SecurityHeadersMiddleware.cs`

## Configuration Updates

### 📋 Program.cs Updates

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* JWT configuration */ });

// SignalR
builder.Services.AddSignalR();

// Rate limiting
builder.Services.AddRateLimiter(/* rate limiting configuration */);

// API documentation
builder.Services.AddSwaggerGen(/* Swagger configuration */);

var app = builder.Build();

// Configure pipeline
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameHub>("/gameHub");

app.Run();
```

### 📋 appsettings.json Updates

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=minesweeper_dev.db"
  },
  "Database": {
    "Provider": "SQLite",
    "EnableRetryOnFailure": true,
    "EnableSensitiveDataLogging": true,
    "EnableDetailedErrors": true
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-here-at-least-32-characters",
    "Issuer": "MinesweeperAPI",
    "Audience": "MinesweeperClient",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "RateLimiting": {
    "AuthPolicy": {
      "PermitLimit": 5,
      "WindowMinutes": 1
    },
    "GameplayPolicy": {
      "PermitLimit": 60,
      "WindowMinutes": 1
    }
  },
  "AntiCheat": {
    "MinMoveTimeMs": 50,
    "MaxMovesPerSecond": 20,
    "SuspiciousPatternThreshold": 5
  }
}
```

## Testing Strategy

### 📋 Unit Tests

- **Authentication Service**: Registration, login, token refresh
- **Anti-Cheat Service**: Pattern detection, timing validation
- **Validators**: All FluentValidation rules

### 📋 Integration Tests

- **API Endpoints**: Full request/response cycle testing
- **SignalR Hubs**: Real-time communication testing
- **Database Operations**: Repository integration with TestContainers

### 📋 End-to-End Tests

- **Game Flow**: Complete game creation, play, and completion
- **Authentication Flow**: Registration, login, and protected endpoints
- **Real-time Features**: Multi-client SignalR scenarios

## Success Criteria

### ✅ Phase 2 Completion Checklist

#### API Integration

- [ ] All controllers use repository-based data access
- [ ] MediatR integration for all operations
- [ ] Proper error handling and validation
- [ ] Idempotency support for critical operations

#### Authentication

- [ ] JWT-based authentication system
- [ ] Secure password hashing (BCrypt)
- [ ] Refresh token functionality
- [ ] Rate limiting on authentication endpoints

#### Real-time Features

- [ ] SignalR hub for game updates
- [ ] Live game state synchronization
- [ ] Player presence tracking
- [ ] Chat functionality (basic)

#### Security

- [ ] Input validation on all endpoints
- [ ] Anti-cheat detection system
- [ ] Rate limiting implementation
- [ ] Security headers configuration

#### Documentation & Testing

- [ ] Complete Swagger/OpenAPI documentation
- [ ] Integration tests for all endpoints
- [ ] SignalR hub testing
- [ ] Performance and load testing

#### Quality Metrics

- [ ] >90% test coverage for new code
- [ ] <200ms average API response time
- [ ] <100ms SignalR message latency
- [ ] Zero security vulnerabilities

## Risk Mitigation

### 🚨 Potential Challenges

1. **Authentication Complexity**
   - **Risk**: JWT implementation errors, security vulnerabilities
   - **Mitigation**: Use well-tested libraries, follow OWASP guidelines

2. **SignalR Performance**
   - **Risk**: Connection scaling issues, message delivery delays
   - **Mitigation**: Implement connection limits, message queuing

3. **Database Concurrency**
   - **Risk**: Race conditions in game state updates
   - **Mitigation**: Optimistic concurrency control, proper transaction scoping

4. **API Security**
   - **Risk**: Injection attacks, authorization bypass
   - **Mitigation**: Input validation, authorization policies, security testing

## Next Steps After Phase 2

### 🔮 Phase 3 Preview: Advanced Features

- **Multiplayer Modes**: Competitive and cooperative gameplay
- **Advanced Analytics**: Player behavior analysis, skill assessment
- **Social Features**: Friends, achievements, tournaments
- **Mobile API**: Optimized endpoints for mobile clients
- **Microservices**: Decomposition for scaling
- **Cloud Deployment**: Azure/AWS containerized deployment

## Timeline Estimate

| Task                   | Priority | Time Estimate | Dependencies        |
| ---------------------- | -------- | ------------- | ------------------- |
| Controller Integration | High     | 2-3 hours     | Phase 1 complete    |
| Authentication System  | High     | 3-4 hours     | Controllers updated |
| Input Validation       | High     | 2-3 hours     | Controllers + Auth  |
| Security Hardening     | High     | 2-3 hours     | All above           |
| SignalR Features       | Medium   | 2-3 hours     | Auth system         |
| API Documentation      | Medium   | 1-2 hours     | All endpoints       |

**Total Estimated Time: 12-18 hours**

## Implementation Notes

### 🔧 Development Guidelines

- **Security First**: All endpoints require authentication except registration/login
- **Performance Focus**: Optimize for <200ms API response times
- **Error Handling**: Standardized error responses with proper HTTP status codes
- **Testing**: Write tests before implementation (TDD approach)
- **Documentation**: XML comments for all public APIs

### 📚 Resources

- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [Entity Framework Best Practices](https://docs.microsoft.com/en-us/ef/core/miscellaneous/best-practices)
- [FluentValidation Guide](https://docs.fluentvalidation.net/)

---

This Phase 2 plan builds upon our solid Phase 1 foundation to create a production-ready minesweeper web application with enterprise-grade security, real-time features, and comprehensive testing.
