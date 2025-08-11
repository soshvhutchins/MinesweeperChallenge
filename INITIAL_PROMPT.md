# GitHub Copilot Prompt: Minesweeper Game in .NET 9

Create a complete Minesweeper game clone using .NET 9 with Clean Architecture, Domain-Driven Design (DDD), and CQRS patterns. Follow these specific requirements:

## Architecture Requirements

**Layer Structure:**

- `Domain/` - Pure business logic, entities, value objects, domain events
- `Application/` - CQRS handlers (commands/queries), use cases, interfaces
- `Infrastructure/` - Entity Framework, repositories, external services
- `Presentation/` - Controllers, minimal APIs, SignalR hubs

**Key Patterns:**

- Clean Architecture with dependency inversion
- CQRS using MediatR for commands and queries
- Domain-Driven Design with rich domain models
- Repository pattern with security-first design
- Value objects for type safety (GameId, PlayerId, CellPosition, etc.)

## Domain Model Requirements

**Core Entities:**

- `Game` aggregate root with game state, board dimensions, mine count
- `GameBoard` entity containing cells and game logic
- `Cell` entity with position, state (hidden/revealed/flagged), mine status
- `Player` entity for tracking games and statistics

**Value Objects:**

- `CellPosition` (row, column coordinates)
- `GameDifficulty` (beginner/intermediate/expert with dimensions/mines)
- `GameStatus` (NotStarted/InProgress/Won/Lost)
- `CellState` (Hidden/Revealed/Flagged)

**Domain Events:**

- `GameStartedEvent`, `CellRevealedEvent`, `GameWonEvent`, `GameLostEvent`, `CellFlaggedEvent`

## CQRS Implementation

**Commands (State Changes):**

- `StartNewGameCommand` - Initialize new game with difficulty
- `RevealCellCommand` - Reveal cell at position with cascade logic
- `FlagCellCommand` - Toggle flag on cell
- `RestartGameCommand` - Reset current game

**Queries (Data Retrieval):**

- `GetGameByIdQuery` - Retrieve game state
- `GetGameBoardQuery` - Get current board state for UI
- `GetPlayerStatisticsQuery` - Player win/loss records
- `GetGameHistoryQuery` - Previous games for player

**Command/Query Handlers:**

- Each command/query has dedicated handler with validation
- Use MediatR for request/response patterns

## Technical Implementation

**Entity Framework Setup:**

- Use Code First with migrations
- Configure entity relationships and constraints
- Implement value object conversions
- Add audit fields (CreatedTime, UpdatedTime, SyncToken)

**Repository Pattern:**

- `IGameRepository` with security-first design
- Implement conditional operations for idempotency
- Use UserContext for authorization where needed
- Include internal methods for system operations

**API Design:**

- RESTful controllers following HTTP conventions
- Support idempotency headers for state changes
- Include proper error handling and validation
- Return appropriate HTTP status codes

## Game Logic Requirements

**Core Gameplay:**

- Generate random mine placement avoiding first click
- Implement flood-fill algorithm for revealing empty cells
- Calculate adjacent mine counts for numbered cells
- Track game timer and move count
- Auto-reveal when all safe cells found

**Difficulty Levels:**

- Beginner: 9x9 grid, 10 mines
- Intermediate: 16x16 grid, 40 mines  
- Expert: 30x16 grid, 99 mines
- Custom: User-defined dimensions and mine count

**Win/Loss Conditions:**

- Win: All non-mine cells revealed
- Loss: Mine cell revealed
- Track statistics and best times

## Testing Strategy

**Unit Tests:**

- Domain logic and business rules (>90% coverage)
- Command/query handlers with mocked dependencies
- Value object validation and behavior
- Game logic algorithms (mine placement, flood-fill)

**Integration Tests:**

- Repository operations with test database
- API endpoints with WebApplicationFactory
- Complete game flow scenarios

**Test Organization:**

- `ProjectName.UnitTests/` - Fast, isolated domain tests
- `ProjectName.IntegrationTests/` - Database and API tests
- Use xUnit, FluentAssertions, Moq/NSubstitute

## Project Structure

```plaintext
src/
├── Minesweeper.Domain/
│   ├── Entities/           # Game, GameBoard, Cell, Player
│   ├── ValueObjects/       # CellPosition, GameDifficulty, etc.
│   ├── Events/             # Domain events
│   └── Interfaces/         # Domain service interfaces
├── Minesweeper.Application/
│   ├── Commands/           # Game commands and handlers
│   ├── Queries/            # Game queries and handlers
│   └── Interfaces/         # Repository interfaces
├── Minesweeper.Infrastructure/
│   ├── Data/               # EF DbContext and configurations
│   ├── Repositories/       # Repository implementations
│   └── Services/           # External service implementations
└── Minesweeper.WebApi/
    ├── Controllers/        # Game API controllers
    └── Hubs/              # SignalR for real-time updates
```

## Additional Requirements

**Security & Validation:**

- Input validation for all commands
- Prevent cheating through API manipulation
- Secure game state persistence
- Rate limiting for API endpoints

**Performance:**

- Efficient algorithms for large boards
- Optimize database queries
- Consider caching for frequently accessed data
- Async/await throughout

**Error Handling:**

- Domain exceptions for business rule violations
- Proper HTTP error responses
- Logging for troubleshooting
- Graceful degradation

**Documentation:**

- XML documentation for public APIs
- README with setup instructions
- API documentation (Swagger/OpenAPI)
- Architecture decision records

Start with the domain model and work outward. Create entities first, then value objects, then repositories, then command/query handlers, and finally the API controllers. Include comprehensive unit tests for each component as you build it.

Focus on making the domain model rich with behavior, not anemic data containers. Ensure all business rules are enforced at the domain level and use the type system to prevent invalid states.
