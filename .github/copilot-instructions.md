# GitHub Copilot Instructions for Minesweeper Game Project

## Project Overview

This is a **complete Minesweeper game implementation** using .NET 9+ that demonstrates **enterprise-grade game development** with modern architectural patterns. The project serves as both a functional game and an educational template for learning Clean Architecture, Domain-Driven Design (DDD), and CQRS patterns in a gaming context.

### Key Features

- **Clean Architecture**: Strict layer separation with dependency inversion
- **Domain-Driven Design**: Rich game models with proper business logic encapsulation
- **CQRS + MediatR**: Command/Query separation for game operations
- **Entity Framework Core**: Database persistence with migrations
- **Comprehensive Testing**: Unit, integration, and architecture tests
- **Security-First**: Player-based authorization and anti-cheat considerations
- **Real-Time Ready**: SignalR infrastructure for multiplayer features

### Game Implementation

- **Complete Minesweeper Logic**: Mine placement, cell revealing, flood-fill algorithm
- **Multiple Difficulty Levels**: Beginner, Intermediate, Expert, and Custom configurations
- **Game State Management**: Start, pause, resume, win/loss detection
- **Player Statistics**: Game history, win rates, performance tracking
- **RESTful API**: Comprehensive game management endpoints with Swagger documentation

### Educational Value

This project demonstrates how to build production-ready games using:

- Clean Architecture principles in game development
- Domain modeling for game entities and business rules
- CQRS patterns for game commands and queries
- Security patterns for multiplayer game data
- Testing strategies for complex game logic

### Essential Commands

```bash
# Build and test (use VS Code tasks via Ctrl+Shift+P → "Tasks: Run Task")
dotnet build                                    # Build solution
dotnet test                                     # Run all tests
dotnet run --project src/Minesweeper.WebApi   # Run minesweeper web API
dotnet ef migrations add <name> --project src/Minesweeper.Infrastructure --startup-project src/Minesweeper.WebApi # Add EF Core migration
dotnet ef database update --project src/Minesweeper.Infrastructure --startup-project src/Minesweeper.WebApi       # Apply migrations
dotnet format                                  # Format code
```

### VS Code Task Integration

**Recommended Development Workflow** (Ctrl+Shift+P → "Tasks: Run Task"):

- **Development**: `run-api-with-swagger` - Start API with Swagger UI
- **Testing**: `test-unit` for quick feedback, `test-integration` for full validation
- **Database**: `ef-migrations-add` → `ef-database-update` for schema changes
- **Code Quality**: `format` for consistent styling
- **Hot Reload**: `run-watch` for continuous development

## Architecture Overview

This template implements a **layered architecture** with strict dependency rules:

### Layer Dependencies (Inner → Outer)

```text
Domain ← Application ← Infrastructure ← Presentation
```

- **Domain Layer**: Game entities, board logic, mine placement, winning conditions
- **Application Layer**: Game commands/queries, player statistics, game flow orchestration
- **Infrastructure Layer**: Entity Framework, game state persistence, player repositories
- **Presentation Layer**: Game API controllers, SignalR hubs for real-time gameplay

### Key Patterns Used

- **Clean Architecture**: Dependency inversion with game domain at the center
- **CQRS**: Game commands (reveal/flag cells) and queries (get game state) using MediatR
- **DDD**: Rich game models, cell aggregates, position value objects, game events
- **Repository Pattern**: Secure game data access with player-based authorization

## Development Workflows

## Development Workflows

### VS Code Configuration

- **Launch configurations**: Debug game API, console, and test projects
- **Tasks**: Pre-configured for build, test, EF migrations, Docker
- **Forest Green theme**: Custom Pantone gradient workspace theming
- **MCP servers**: GitHub, Microsoft Docs, Context7 integration

## Code Generation Guidelines

## Code Generation Guidelines

### Game Entity Creation (Domain Layer)

When creating game domain entities, follow this pattern:

```csharp
public class Game : Entity<GameId>
{
    private Game() { } // EF Core constructor

    public PlayerId PlayerId { get; private set; }
    public GameDifficulty Difficulty { get; private set; }
    public GameStatus Status { get; private set; }
    public GameBoard Board { get; private set; }
    public DateTime StartTime { get; private set; }
    public int MoveCount { get; private set; }

    public static Game Create(GameId gameId, PlayerId playerId, GameDifficulty difficulty)
    {
        var game = new Game
        {
            Id = gameId,
            PlayerId = playerId,
            Difficulty = difficulty,
            StartTime = DateTime.UtcNow,
            Status = GameStatus.NotStarted
        };

        game.Board = GameBoard.Create(difficulty.Width, difficulty.Height, difficulty.MineCount);
        game.RaiseDomainEvent(new GameCreatedEvent(gameId, playerId));

        return game;
    }

    public Result RevealCell(CellPosition position)
    {
        if (Status != GameStatus.InProgress && Status != GameStatus.NotStarted)
            return Result.Failure("Cannot reveal cells in finished game");

        // Auto-start game on first reveal
        if (Status == GameStatus.NotStarted)
        {
            Board.GenerateMines(position);
            Status = GameStatus.InProgress;
            RaiseDomainEvent(new GameStartedEvent(Id, PlayerId, DateTime.UtcNow));
        }

        var revealResult = Board.RevealCell(position);
        if (revealResult.IsFailure)
            return revealResult;

        MoveCount++;
        var cell = Board.GetCell(position);
        RaiseDomainEvent(new CellRevealedEvent(Id, position, cell.HasMine));

                // Check win/loss conditions
        if (cell.HasMine)
        {
            Status = GameStatus.Lost;
            RaiseDomainEvent(new GameLostEvent(Id, PlayerId, position));
        }
        else if (Board.IsGameWon())
        {
            Status = GameStatus.Won;
            RaiseDomainEvent(new GameWonEvent(Id, PlayerId, ElapsedTime!.Value, MoveCount));
        }

        return Result.Success();
    }
}
```

### CQRS Implementation (Application Layer)

Always use MediatR for game commands and queries:

```csharp
// Command for game state changes
public record StartNewGameCommand(
    PlayerId PlayerId,
    GameDifficulty Difficulty,
    string? IdempotencyKey = null
) : IRequest<Result<GameId>>;

public class StartNewGameHandler : IRequestHandler<StartNewGameCommand, Result<GameId>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IPlayerRepository _playerRepository;

    public async Task<Result<GameId>> Handle(StartNewGameCommand request, CancellationToken cancellationToken)
    {
        // Check for idempotency
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existingGame = await _gameRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
            if (existingGame != null)
                return Result.Success(existingGame.Id);
        }

        // Validate player exists
        var playerExists = await _playerRepository.ExistsAsync(request.PlayerId, cancellationToken);
        if (!playerExists)
            return Result.Failure<GameId>("Player not found");

        // Create new game
        var gameId = GameId.New();
        var game = Game.Create(gameId, request.PlayerId, request.Difficulty);
        await _gameRepository.SaveAsync(game, cancellationToken);

        return Result.Success(gameId);
    }
}

// Query for game data retrieval
public record GetGameByIdQuery(GameId GameId, PlayerId PlayerId) : IRequest<Result<GameDto>>;

public class GetGameByIdHandler : IRequestHandler<GetGameByIdQuery, Result<GameDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;

    public async Task<Result<GameDto>> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
    {
        var game = await _gameRepository.GetByIdAsync(request.GameId, cancellationToken);
        if (game == null)
            return Result.Failure<GameDto>("Game not found");

        // Security: Only allow players to access their own games
        if (game.PlayerId != request.PlayerId)
            return Result.Failure<GameDto>("Unauthorized");

        var gameDto = _mapper.Map<GameDto>(game);
        return Result.Success(gameDto);
    }
}
```

### Repository Implementation (Infrastructure Layer)

```csharp
public interface IGameRepository // In Application layer
{
    Task<Game?> GetByIdAsync(GameId gameId, CancellationToken cancellationToken = default);
    Task<Game?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IEnumerable<Game>> GetByPlayerIdAsync(PlayerId playerId, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task SaveAsync(Game game, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(GameId gameId, CancellationToken cancellationToken = default);
}

public class EfGameRepository : IGameRepository // In Infrastructure layer
{
    private readonly ApplicationDbContext _context;

    public async Task<Game?> GetByIdAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Include(g => g.Board)
            .FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);
    }

    public async Task SaveAsync(Game game, CancellationToken cancellationToken = default)
    {
        var existingGame = await _context.Games.FindAsync(game.Id);
        if (existingGame == null)
            _context.Games.Add(game);
        else
            _context.Entry(existingGame).CurrentValues.SetValues(game);

        await _context.SaveChangesAsync(cancellationToken);

        // Clear domain events after successful save
        game.ClearDomainEvents();
    }
}
```

### Controller Patterns (Presentation Layer)

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
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(GetGame), new { id = result.Value }, result.Value);
    }

    [HttpPost("{id}/reveal")]
    public async Task<ActionResult<RevealCellResult>> RevealCell(
        Guid id,
        [FromBody] RevealCellRequest request)
    {
        var playerId = GetCurrentPlayerId();
        var command = new RevealCellCommand(
            new GameId(id),
            new CellPosition(request.Row, request.Column),
            playerId);

        var result = await _mediator.Send(command);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(result.Value);
    }
}
```

## Project-Specific Conventions

### Naming Patterns

- **Entities**: `Game`, `GameBoard`, `Cell`, `Player` (game domain names)
- **Value Objects**: `CellPosition`, `GameDifficulty`, `GameStatus`, `CellState`
- **Commands**: `StartNewGameCommand`, `RevealCellCommand`, `FlagCellCommand`
- **Queries**: `GetGameByIdQuery`, `GetPlayerStatisticsQuery`, `GetGameHistoryQuery`
- **Handlers**: `StartNewGameHandler`, `GetGameByIdHandler`
- **DTOs**: `GameDto`, `PlayerDto`, `CellDto` (for API responses)

### Directory Structure

```text
src/
├── Minesweeper.Domain/
│   ├── Entities/          # Game, GameBoard, Cell, Player
│   ├── ValueObjects/      # CellPosition, GameDifficulty, etc.
│   ├── Events/           # Game domain events
│   └── Common/           # Entity base classes, Result pattern
├── Minesweeper.Application/
│   ├── Commands/         # Game commands and handlers
│   ├── Queries/          # Game queries and handlers
│   ├── DTOs/            # Data transfer objects
│   └── Interfaces/       # Repository interfaces
├── Minesweeper.Infrastructure/
│   ├── Data/            # Entity Framework DbContext
│   ├── Repositories/    # Repository implementations
│   └── Configurations/  # EF entity configurations
└── Minesweeper.WebApi/
    ├── Controllers/     # Game API controllers
    ├── Hubs/           # SignalR game hubs
    └── Models/         # Request/response models
```

### Testing Strategy

- **Unit Tests**: Game logic and business rules (mine placement, flood-fill algorithm)
- **Integration Tests**: Repository operations and complete game flow scenarios
- **Architecture Tests**: Clean Architecture compliance (ArchUnitNET)

```csharp
public class Game : Entity<GameId>
{
    public static Game Create(GameId gameId, PlayerId playerId, GameDifficulty difficulty)
    {
        // Domain validation and business rules
        var game = new Game
        {
            Id = gameId,
            PlayerId = playerId,
            Difficulty = difficulty,
            Status = GameStatus.NotStarted,
            StartTime = DateTime.UtcNow
        };

        game.RaiseDomainEvent(new GameCreatedEvent(gameId, playerId));
        return game;
    }

    public Result RevealCell(CellPosition position)
    {
        if (Status != GameStatus.InProgress && Status != GameStatus.NotStarted)
            return Result.Failure("Cannot reveal cells in finished game");

        var revealResult = Board.RevealCell(position);
        if (revealResult.IsFailure)
            return revealResult;

        RaiseDomainEvent(new CellRevealedEvent(Id, position, Board.GetCell(position).HasMine));
        return Result.Success();
    }
}
```

### CQRS Implementation (Application Layer)

Always use MediatR for game commands and queries:

```csharp
// Command for game state changes
public record StartNewGameCommand(
    PlayerId PlayerId,
    GameDifficulty Difficulty,
    string? IdempotencyKey = null
) : IRequest<Result<GameId>>;

public class StartNewGameHandler : IRequestHandler<StartNewGameCommand, Result<GameId>>
{
    private readonly IGameRepository _gameRepository;

    public async Task<Result<GameId>> Handle(StartNewGameCommand request, CancellationToken cancellationToken)
    {
        // Validate player and create new game
        var gameId = GameId.New();
        var game = Game.Create(gameId, request.PlayerId, request.Difficulty);
        await _gameRepository.SaveAsync(game, cancellationToken);

        return Result.Success(gameId);
    }
}

// Query for game data retrieval
public record GetGameByIdQuery(GameId GameId, PlayerId PlayerId) : IRequest<Result<GameDto>>;

public class GetGameByIdHandler : IRequestHandler<GetGameByIdQuery, Result<GameDto>>
{
    private readonly IGameRepository _gameRepository;

    public async Task<Result<GameDto>> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
    {
        var game = await _gameRepository.GetByIdAsync(request.GameId, cancellationToken);
        if (game == null)
            return Result.Failure<GameDto>("Game not found");

        // Security: Only allow players to access their own games
        if (game.PlayerId != request.PlayerId)
            return Result.Failure<GameDto>("Unauthorized");

        return Result.Success(MapToDto(game));
    }
}
```

## Technology Stack

### Core Framework

- **.NET 9+**: Latest LTS version with modern C# features
- **Entity Framework Core**: For data access with migrations
- **MediatR**: CQRS implementation with request/response patterns

### Game-Specific Libraries

- **FluentValidation**: Input validation for game commands
- **AutoMapper**: Object-to-object mapping for DTOs
- **SignalR**: Real-time game updates and multiplayer support
- **xUnit**: Testing framework with FluentAssertions

### Development Tools

- **NSubstitute**: Mocking framework for unit tests
- **TestContainers**: Integration testing with real databases
- **ArchUnitNET**: Architecture compliance testing

### VS Code Integration

- **Forest Green Theme**: Custom Pantone gradient workspace theme
- **FiraCode Nerd Font**: Programming font with ligatures
- **GitHub Copilot**: Advanced AI code completion
- **MCP Servers**: GitHub, Microsoft Docs, Context7 context providers

## Game-Specific Implementation Guidelines

### Game Logic Patterns

When implementing core game mechanics, follow these patterns:

```csharp
// Mine placement with first-click safety
public void GenerateMines(CellPosition firstClickPosition)
{
    if (MinesGenerated) return;

    var availablePositions = GetAllPositions()
        .Where(pos => !pos.Equals(firstClickPosition))
        .OrderBy(_ => _random.Next())
        .Take(MineCount)
        .ToList();

    foreach (var position in availablePositions)
    {
        _cells[position.Row, position.Column].SetMine();
    }

    CalculateAdjacentMineCounts();
    MinesGenerated = true;
}

// Flood-fill algorithm for revealing empty cells
private void RevealAdjacentCells(CellPosition position)
{
    var adjacentPositions = position.GetAdjacentPositions()
        .Where(IsValidPosition)
        .ToList();

    foreach (var adjacentPosition in adjacentPositions)
    {
        var adjacentCell = _cells[adjacentPosition.Row, adjacentPosition.Column];
        if (adjacentCell.State == CellState.Hidden)
        {
            adjacentCell.Reveal();

            if (adjacentCell.AdjacentMineCount == 0 && !adjacentCell.HasMine)
            {
                RevealAdjacentCells(adjacentPosition); // Recursive reveal
            }
        }
    }
}
```

### Security Patterns for Game Data

```csharp
// Always validate player ownership
public async Task<Result<GameDto>> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
{
    var game = await _gameRepository.GetByIdAsync(request.GameId, cancellationToken);
    if (game == null)
        return Result.Failure<GameDto>("Game not found");

    // Security: Only allow players to access their own games
    if (game.PlayerId != request.PlayerId)
        return Result.Failure<GameDto>("Unauthorized");

    var gameDto = _mapper.Map<GameDto>(game);
    return Result.Success(gameDto);
}
```

### Value Object Implementations

```csharp
// Game difficulty with validation
public record GameDifficulty(string Name, int Width, int Height, int MineCount)
{
    public static GameDifficulty Beginner => new("Beginner", 9, 9, 10);
    public static GameDifficulty Intermediate => new("Intermediate", 16, 16, 40);
    public static GameDifficulty Expert => new("Expert", 30, 16, 99);

    public static GameDifficulty Custom(int width, int height, int mineCount)
    {
        ValidateDimensions(width, height, mineCount);
        return new GameDifficulty("Custom", width, height, mineCount);
    }

    private static void ValidateDimensions(int width, int height, int mineCount)
    {
        if (width < 1 || height < 1)
            throw new ArgumentException("Board dimensions must be positive");
        if (mineCount >= width * height)
            throw new ArgumentException("Mine count must be less than total cells");
    }
}

// Cell position with adjacent cell calculation
public record CellPosition(int Row, int Column)
{
    public IEnumerable<CellPosition> GetAdjacentPositions()
    {
        for (int r = Row - 1; r <= Row + 1; r++)
        {
            for (int c = Column - 1; c <= Column + 1; c++)
            {
                if (r != Row || c != Column)
                    yield return new CellPosition(r, c);
            }
        }
    }
}
```

## Stub and Partial Implementation Guidelines

**Production-First Philosophy**: All implementations should be production-ready with enterprise-grade security, performance, and compliance. However, when partial implementations are necessary during development, they must be clearly marked with proper TODO syntax to ensure they are completed before production deployment.

All incomplete implementations must be clearly marked with proper TODO syntax:

```csharp
// TODO: [FULL_IMPLEMENTATION] Implement advanced game statistics and analytics
// Description: Add intelligent analytics for player behavior, game difficulty adjustment,
// and personalized recommendations based on play patterns and success rates.
// Current: Basic win/loss tracking without advanced metrics
// Required: Player skill assessment, adaptive difficulty, engagement analytics
public async Task<PlayerAnalyticsDto> GetPlayerAnalyticsAsync(PlayerId playerId, CancellationToken cancellationToken = default)
{
    // Current: Simple statistics implementation
    var player = await _playerRepository.GetByIdAsync(playerId, cancellationToken);
    if (player == null)
        return null;

    // TODO: Replace with advanced analytics pipeline:
    // 1. Play pattern analysis (time between moves, common mistake patterns)
    // 2. Skill progression tracking over time
    // 3. Adaptive difficulty recommendations
    // 4. Engagement prediction and retention modeling
    // 5. Social features (leaderboards, achievements)

    return new PlayerAnalyticsDto
    {
        TotalGames = player.Statistics.TotalGames,
        WinRate = player.Statistics.WinPercentage,
        // TODO: Add advanced metrics here
    };
}

// TODO: [SECURITY] Implement comprehensive audit trail for all game actions
// Description: Add detailed logging for anti-cheat detection, player behavior analysis,
// and compliance requirements including move timing, pattern detection, and anomaly alerts.
public async Task<Result> LogGameActionAsync(GameId gameId, PlayerId playerId, GameAction action, CancellationToken cancellationToken = default)
{
    // TODO: Add comprehensive audit logging:
    // 1. Log all player actions with precise timestamps
    // 2. Detect suspicious patterns (impossible reaction times, perfect play)
    // 3. Store client-side validation data for cheat detection
    // 4. Generate behavioral fingerprints for account security
    // 5. Implement tamper-proof audit storage

    // Current: Basic action logging
    _logger.LogInformation("Player {PlayerId} performed {Action} on game {GameId}",
        playerId, action, gameId);

    return Result.Success();
}

// TODO: [ENHANCEMENT] Implement intelligent hint system
// Description: Add smart hint generation that helps players learn Minesweeper strategies
// without giving away solutions, including educational content and skill building.
public async Task<GameHintDto> GenerateHintAsync(GameId gameId, PlayerId playerId, CancellationToken cancellationToken = default)
{
    // Current: No hint system implemented
    // TODO: Enhance with:
    // 1. Pattern recognition for logical deduction hints
    // 2. Educational explanations of Minesweeper strategies
    // 3. Adaptive hint difficulty based on player skill level
    // 4. Achievement system for solving without hints
    // 5. Tutorial mode with progressive difficulty

    return new GameHintDto
    {
        Message = "Look for cells with numbers that match their adjacent flags",
        // TODO: Add intelligent hint generation
    };
}
```

### TODO Categories

**[FULL_IMPLEMENTATION]**: Complete feature implementation required

- Missing core game mechanics or advanced features
- Incomplete analytics or player progression systems
- Simplified algorithms that need production-grade optimization
- Missing social features or competitive elements

**[SECURITY]**: Security-related implementations required

- Anti-cheat detection and prevention
- Audit logging for compliance and monitoring
- Rate limiting and abuse prevention
- Data privacy and protection features

**[ENHANCEMENT]**: Performance or usability improvements

- Advanced AI for hint generation and tutorials
- Real-time multiplayer game modes
- Performance optimization for large game boards
- Advanced UI/UX features and accessibility

**[ERROR_HANDLING]**: Robust error handling required

- Network failure recovery for real-time features
- Game state corruption detection and recovery
- Graceful degradation under high load
- Client-side validation and synchronization

**[TESTING]**: Test coverage gaps

- Game logic edge cases and boundary conditions
- Performance tests for concurrent players
- Security tests for anti-cheat mechanisms
- End-to-end game flow testing

### Format Requirements

```csharp
// TODO: [CATEGORY] Brief description
// Description: Detailed explanation of what needs to be implemented
// Current: Description of current state/limitations
// Required: Specific requirements or acceptance criteria
public ReturnType MethodSignature()
{
    // Current implementation with clear limitations
    // TODO: Inline comments for specific improvement points
}
```

## Template Customization

When using this project as a template for other games:

1. **Update game domain**: Replace `Game`, `GameBoard`, `Cell` with your game entities
2. **Customize game logic**: Modify rules, scoring, and win conditions
3. **Adapt value objects**: Create game-specific value objects for positions, states, etc.
4. **Update theme**: Modify `.vscode/settings.json` color scheme for your game
5. **Configure MCP**: Set environment variables for AI context providers

Focus on maintaining architectural boundaries and following the established patterns
for consistency and maintainability while adapting the game-specific business logic
to your domain.
