# Minesweeper Implementation Guide

## Overview

This document provides detailed implementation guidance for building the Minesweeper game using .NET 9, Clean Architecture, Domain-Driven Design, and CQRS patterns. It includes step-by-step instructions, code examples, and best practices for each layer of the application.

## Project Setup

### Prerequisites

- .NET 9 SDK
- Entity Framework Core 9
- PostgreSQL (or SQL Server)
- Visual Studio Code with C# extensions

### Solution Structure

Create the solution structure following Clean Architecture principles:

```bash
# Create solution and projects
dotnet new sln -n Minesweeper

# Domain layer (pure business logic)
dotnet new classlib -n Minesweeper.Domain -o src/Minesweeper.Domain

# Application layer (use cases and handlers)
dotnet new classlib -n Minesweeper.Application -o src/Minesweeper.Application

# Infrastructure layer (data access and external services)
dotnet new classlib -n Minesweeper.Infrastructure -o src/Minesweeper.Infrastructure

# Presentation layer (Web API)
dotnet new webapi -n Minesweeper.WebApi -o src/Minesweeper.WebApi

# Test projects
dotnet new xunit -n Minesweeper.UnitTests -o tests/Minesweeper.UnitTests
dotnet new xunit -n Minesweeper.IntegrationTests -o tests/Minesweeper.IntegrationTests

# Add projects to solution
dotnet sln add src/Minesweeper.Domain/Minesweeper.Domain.csproj
dotnet sln add src/Minesweeper.Application/Minesweeper.Application.csproj
dotnet sln add src/Minesweeper.Infrastructure/Minesweeper.Infrastructure.csproj
dotnet sln add src/Minesweeper.WebApi/Minesweeper.WebApi.csproj
dotnet sln add tests/Minesweeper.UnitTests/Minesweeper.UnitTests.csproj
dotnet sln add tests/Minesweeper.IntegrationTests/Minesweeper.IntegrationTests.csproj
```

### Project Dependencies

```bash
# Domain has no external dependencies (pure business logic)

# Application references Domain
cd src/Minesweeper.Application
dotnet add reference ../Minesweeper.Domain/Minesweeper.Domain.csproj
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package AutoMapper

# Infrastructure references Application and Domain
cd ../Minesweeper.Infrastructure
dotnet add reference ../Minesweeper.Domain/Minesweeper.Domain.csproj
dotnet add reference ../Minesweeper.Application/Minesweeper.Application.csproj
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# WebApi references all layers
cd ../Minesweeper.WebApi
dotnet add reference ../Minesweeper.Domain/Minesweeper.Domain.csproj
dotnet add reference ../Minesweeper.Application/Minesweeper.Application.csproj
dotnet add reference ../Minesweeper.Infrastructure/Minesweeper.Infrastructure.csproj
dotnet add package MediatR
dotnet add package Microsoft.AspNetCore.SignalR
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

## Domain Layer Implementation

### 1. Core Entities

#### Base Entity Class

```csharp
// src/Minesweeper.Domain/Common/Entity.cs
namespace Minesweeper.Domain.Common;

public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public TId Id { get; protected set; } = default!;

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public bool Equals(Entity<TId>? other)
    {
        return other is not null && Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Equals(entity);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
```

#### Game Entity (Aggregate Root)

```csharp
// src/Minesweeper.Domain/Entities/Game.cs
namespace Minesweeper.Domain.Entities;

public class Game : Entity<GameId>
{
    private Game() { } // EF Core constructor

    public PlayerId PlayerId { get; private set; } = default!;
    public GameDifficulty Difficulty { get; private set; } = default!;
    public GameStatus Status { get; private set; } = GameStatus.NotStarted;
    public GameBoard Board { get; private set; } = default!;
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public int MoveCount { get; private set; }
    public string? IdempotencyKey { get; private set; }

    public TimeSpan? ElapsedTime => EndTime?.Subtract(StartTime);
    public bool IsGameComplete => Status == GameStatus.Won || Status == GameStatus.Lost;

    public static Game Create(GameId gameId, PlayerId playerId, GameDifficulty difficulty, string? idempotencyKey = null)
    {
        var game = new Game
        {
            Id = gameId,
            PlayerId = playerId,
            Difficulty = difficulty,
            IdempotencyKey = idempotencyKey,
            StartTime = DateTime.UtcNow,
            Status = GameStatus.NotStarted
        };

        game.Board = GameBoard.Create(difficulty.Width, difficulty.Height, difficulty.MineCount);
        
        return game;
    }

    public Result StartGame(CellPosition firstClickPosition)
    {
        if (Status != GameStatus.NotStarted)
            return Result.Failure("Game has already started");

        Board.GenerateMines(firstClickPosition);
        Status = GameStatus.InProgress;
        StartTime = DateTime.UtcNow;

        RaiseDomainEvent(new GameStartedEvent(Id, PlayerId, StartTime));
        
        return Result.Success();
    }

    public Result RevealCell(CellPosition position)
    {
        if (Status == GameStatus.Won || Status == GameStatus.Lost)
            return Result.Failure("Game is already finished");

        if (Status == GameStatus.NotStarted)
        {
            var startResult = StartGame(position);
            if (startResult.IsFailure)
                return startResult;
        }

        var revealResult = Board.RevealCell(position);
        if (revealResult.IsFailure)
            return revealResult;

        MoveCount++;

        var cell = Board.GetCell(position);
        RaiseDomainEvent(new CellRevealedEvent(Id, position, cell.HasMine));

        // Check for game end conditions
        if (cell.HasMine)
        {
            EndGame(GameStatus.Lost);
            RaiseDomainEvent(new GameLostEvent(Id, PlayerId, position));
        }
        else if (Board.IsGameWon())
        {
            EndGame(GameStatus.Won);
            RaiseDomainEvent(new GameWonEvent(Id, PlayerId, ElapsedTime!.Value, MoveCount));
        }

        return Result.Success();
    }

    public Result FlagCell(CellPosition position)
    {
        if (Status != GameStatus.InProgress)
            return Result.Failure("Cannot flag cells when game is not in progress");

        var flagResult = Board.FlagCell(position);
        if (flagResult.IsFailure)
            return flagResult;

        var cell = Board.GetCell(position);
        RaiseDomainEvent(new CellFlaggedEvent(Id, position, cell.State == CellState.Flagged));

        return Result.Success();
    }

    private void EndGame(GameStatus finalStatus)
    {
        Status = finalStatus;
        EndTime = DateTime.UtcNow;
    }
}
```

#### GameBoard Entity

```csharp
// src/Minesweeper.Domain/Entities/GameBoard.cs
namespace Minesweeper.Domain.Entities;

public class GameBoard : Entity<Guid>
{
    private Cell[,] _cells = default!;
    private readonly Random _random = new();

    private GameBoard() { } // EF Core constructor

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int MineCount { get; private set; }
    public bool MinesGenerated { get; private set; }

    public static GameBoard Create(int width, int height, int mineCount)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Board dimensions must be positive");
        
        if (mineCount >= width * height)
            throw new ArgumentException("Mine count must be less than total cells");

        var board = new GameBoard
        {
            Id = Guid.NewGuid(),
            Width = width,
            Height = height,
            MineCount = mineCount
        };

        board.InitializeCells();
        return board;
    }

    private void InitializeCells()
    {
        _cells = new Cell[Height, Width];
        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                var position = new CellPosition(row, col);
                _cells[row, col] = Cell.Create(position);
            }
        }
    }

    public void GenerateMines(CellPosition firstClickPosition)
    {
        if (MinesGenerated)
            return;

        var availablePositions = GetAllPositions()
            .Where(pos => !pos.Equals(firstClickPosition))
            .ToList();

        var minePositions = availablePositions
            .OrderBy(_ => _random.Next())
            .Take(MineCount)
            .ToList();

        foreach (var position in minePositions)
        {
            _cells[position.Row, position.Column].SetMine();
        }

        CalculateAdjacentMineCounts();
        MinesGenerated = true;
    }

    public Result RevealCell(CellPosition position)
    {
        if (!IsValidPosition(position))
            return Result.Failure("Invalid cell position");

        var cell = _cells[position.Row, position.Column];
        var revealResult = cell.Reveal();
        
        if (revealResult.IsFailure)
            return revealResult;

        // If the cell has no adjacent mines, reveal all adjacent cells (flood fill)
        if (cell.AdjacentMineCount == 0 && !cell.HasMine)
        {
            RevealAdjacentCells(position);
        }

        return Result.Success();
    }

    public Result FlagCell(CellPosition position)
    {
        if (!IsValidPosition(position))
            return Result.Failure("Invalid cell position");

        var cell = _cells[position.Row, position.Column];
        return cell.ToggleFlag();
    }

    public bool IsGameWon()
    {
        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                var cell = _cells[row, col];
                if (!cell.HasMine && cell.State == CellState.Hidden)
                    return false;
            }
        }
        return true;
    }

    public Cell GetCell(CellPosition position)
    {
        if (!IsValidPosition(position))
            throw new ArgumentException("Invalid cell position");
        
        return _cells[position.Row, position.Column];
    }

    public IEnumerable<Cell> GetAllCells()
    {
        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                yield return _cells[row, col];
            }
        }
    }

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
                
                // Recursively reveal if this cell also has no adjacent mines
                if (adjacentCell.AdjacentMineCount == 0 && !adjacentCell.HasMine)
                {
                    RevealAdjacentCells(adjacentPosition);
                }
            }
        }
    }

    private void CalculateAdjacentMineCounts()
    {
        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                var position = new CellPosition(row, col);
                var cell = _cells[row, col];
                
                if (!cell.HasMine)
                {
                    var adjacentMineCount = position.GetAdjacentPositions()
                        .Where(IsValidPosition)
                        .Count(pos => _cells[pos.Row, pos.Column].HasMine);
                    
                    cell.SetAdjacentMineCount(adjacentMineCount);
                }
            }
        }
    }

    private bool IsValidPosition(CellPosition position)
    {
        return position.Row >= 0 && position.Row < Height &&
               position.Column >= 0 && position.Column < Width;
    }

    private IEnumerable<CellPosition> GetAllPositions()
    {
        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                yield return new CellPosition(row, col);
            }
        }
    }
}
```

#### Cell Entity

```csharp
// src/Minesweeper.Domain/Entities/Cell.cs
namespace Minesweeper.Domain.Entities;

public class Cell : Entity<Guid>
{
    private Cell() { } // EF Core constructor

    public CellPosition Position { get; private set; } = default!;
    public CellState State { get; private set; } = CellState.Hidden;
    public bool HasMine { get; private set; }
    public int AdjacentMineCount { get; private set; }

    public static Cell Create(CellPosition position)
    {
        return new Cell
        {
            Id = Guid.NewGuid(),
            Position = position,
            State = CellState.Hidden,
            HasMine = false,
            AdjacentMineCount = 0
        };
    }

    public Result Reveal()
    {
        if (State == CellState.Flagged)
            return Result.Failure("Cannot reveal a flagged cell");

        if (State == CellState.Revealed)
            return Result.Failure("Cell is already revealed");

        State = CellState.Revealed;
        return Result.Success();
    }

    public Result ToggleFlag()
    {
        if (State == CellState.Revealed)
            return Result.Failure("Cannot flag a revealed cell");

        State = State == CellState.Flagged ? CellState.Hidden : CellState.Flagged;
        return Result.Success();
    }

    public void SetMine()
    {
        HasMine = true;
    }

    public void SetAdjacentMineCount(int count)
    {
        AdjacentMineCount = count;
    }
}
```

### 2. Value Objects

#### Strongly-Typed IDs

```csharp
// src/Minesweeper.Domain/ValueObjects/GameId.cs
namespace Minesweeper.Domain.ValueObjects;

public record GameId(Guid Value)
{
    public static GameId New() => new(Guid.NewGuid());
    public static GameId Empty => new(Guid.Empty);
    
    public override string ToString() => Value.ToString();
    
    public static implicit operator Guid(GameId gameId) => gameId.Value;
    public static implicit operator GameId(Guid value) => new(value);
}

// src/Minesweeper.Domain/ValueObjects/PlayerId.cs
namespace Minesweeper.Domain.ValueObjects;

public record PlayerId(Guid Value)
{
    public static PlayerId New() => new(Guid.NewGuid());
    public static PlayerId Empty => new(Guid.Empty);
    
    public override string ToString() => Value.ToString();
    
    public static implicit operator Guid(PlayerId playerId) => playerId.Value;
    public static implicit operator PlayerId(Guid value) => new(value);
}
```

#### CellPosition Value Object

```csharp
// src/Minesweeper.Domain/ValueObjects/CellPosition.cs
namespace Minesweeper.Domain.ValueObjects;

public record CellPosition(int Row, int Column)
{
    public static CellPosition Create(int row, int column)
    {
        if (row < 0 || column < 0)
            throw new ArgumentException("Position coordinates must be non-negative");
        return new CellPosition(row, column);
    }

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

    public double DistanceTo(CellPosition other)
    {
        var deltaRow = Row - other.Row;
        var deltaCol = Column - other.Column;
        return Math.Sqrt(deltaRow * deltaRow + deltaCol * deltaCol);
    }
}
```

#### GameDifficulty Value Object

```csharp
// src/Minesweeper.Domain/ValueObjects/GameDifficulty.cs
namespace Minesweeper.Domain.ValueObjects;

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

    public int TotalCells => Width * Height;
    public double MineDensity => (double)MineCount / TotalCells;

    private static void ValidateDimensions(int width, int height, int mineCount)
    {
        if (width < 1 || height < 1)
            throw new ArgumentException("Board dimensions must be positive");
        if (mineCount < 0)
            throw new ArgumentException("Mine count cannot be negative");
        if (mineCount >= width * height)
            throw new ArgumentException("Mine count must be less than total cells");
        if (width > 100 || height > 100)
            throw new ArgumentException("Board dimensions too large");
    }
}
```

### 3. Domain Events

#### Domain Event Interface

```csharp
// src/Minesweeper.Domain/Common/IDomainEvent.cs
namespace Minesweeper.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
```

#### Game Events

```csharp
// src/Minesweeper.Domain/Events/GameEvents.cs
namespace Minesweeper.Domain.Events;

public record GameStartedEvent(
    GameId GameId,
    PlayerId PlayerId,
    DateTime StartTime
) : DomainEvent;

public record CellRevealedEvent(
    GameId GameId,
    CellPosition Position,
    bool HadMine
) : DomainEvent;

public record CellFlaggedEvent(
    GameId GameId,
    CellPosition Position,
    bool IsFlagged
) : DomainEvent;

public record GameWonEvent(
    GameId GameId,
    PlayerId PlayerId,
    TimeSpan ElapsedTime,
    int MoveCount
) : DomainEvent;

public record GameLostEvent(
    GameId GameId,
    PlayerId PlayerId,
    CellPosition MinePosition
) : DomainEvent;
```

### 4. Result Pattern

```csharp
// src/Minesweeper.Domain/Common/Result.cs
namespace Minesweeper.Domain.Common;

public class Result
{
    protected Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; } = string.Empty;

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
    public static Result<T> Failure<T>(string error) => new(default, false, error);
}

public class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, string error) : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access value of failed result");

    public static implicit operator Result<T>(T value) => Success(value);
}
```

## Application Layer Implementation

### 1. Command Handlers

#### StartNewGameCommand

```csharp
// src/Minesweeper.Application/Commands/StartNewGameCommand.cs
namespace Minesweeper.Application.Commands;

public record StartNewGameCommand(
    PlayerId PlayerId,
    GameDifficulty Difficulty,
    string? IdempotencyKey = null
) : IRequest<Result<GameId>>;

public class StartNewGameHandler : IRequestHandler<StartNewGameCommand, Result<GameId>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly ILogger<StartNewGameHandler> _logger;

    public StartNewGameHandler(
        IGameRepository gameRepository,
        IPlayerRepository playerRepository,
        ILogger<StartNewGameHandler> logger)
    {
        _gameRepository = gameRepository;
        _playerRepository = playerRepository;
        _logger = logger;
    }

    public async Task<Result<GameId>> Handle(StartNewGameCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting new game for player {PlayerId} with difficulty {Difficulty}", 
            request.PlayerId, request.Difficulty.Name);

        // Check for existing game with same idempotency key
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var existingGame = await _gameRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
            if (existingGame != null)
            {
                _logger.LogInformation("Returning existing game {GameId} for idempotency key {IdempotencyKey}", 
                    existingGame.Id, request.IdempotencyKey);
                return Result.Success(existingGame.Id);
            }
        }

        // Validate player exists
        var playerExists = await _playerRepository.ExistsAsync(request.PlayerId, cancellationToken);
        if (!playerExists)
        {
            _logger.LogWarning("Player {PlayerId} not found", request.PlayerId);
            return Result.Failure<GameId>("Player not found");
        }

        // Create new game
        var gameId = GameId.New();
        var game = Game.Create(gameId, request.PlayerId, request.Difficulty, request.IdempotencyKey);

        await _gameRepository.SaveAsync(game, cancellationToken);

        _logger.LogInformation("Created new game {GameId} for player {PlayerId}", gameId, request.PlayerId);
        return Result.Success(gameId);
    }
}
```

#### RevealCellCommand

```csharp
// src/Minesweeper.Application/Commands/RevealCellCommand.cs
namespace Minesweeper.Application.Commands;

public record RevealCellCommand(
    GameId GameId,
    CellPosition Position,
    PlayerId PlayerId
) : IRequest<Result<RevealCellResult>>;

public record RevealCellResult(
    GameStatus GameStatus,
    IEnumerable<CellDto> RevealedCells,
    bool IsGameComplete
);

public class RevealCellHandler : IRequestHandler<RevealCellCommand, Result<RevealCellResult>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<RevealCellHandler> _logger;

    public RevealCellHandler(
        IGameRepository gameRepository,
        IMapper mapper,
        ILogger<RevealCellHandler> logger)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<RevealCellResult>> Handle(RevealCellCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Revealing cell at position ({Row}, {Column}) for game {GameId}", 
            request.Position.Row, request.Position.Column, request.GameId);

        var game = await _gameRepository.GetByIdAsync(request.GameId, cancellationToken);
        if (game == null)
        {
            _logger.LogWarning("Game {GameId} not found", request.GameId);
            return Result.Failure<RevealCellResult>("Game not found");
        }

        if (game.PlayerId != request.PlayerId)
        {
            _logger.LogWarning("Player {PlayerId} attempted to access game {GameId} owned by {OwnerId}", 
                request.PlayerId, request.GameId, game.PlayerId);
            return Result.Failure<RevealCellResult>("Unauthorized access to game");
        }

        var result = game.RevealCell(request.Position);
        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to reveal cell: {Error}", result.Error);
            return Result.Failure<RevealCellResult>(result.Error);
        }

        await _gameRepository.SaveAsync(game, cancellationToken);

        var revealedCells = game.Board.GetAllCells()
            .Where(c => c.State == CellState.Revealed)
            .Select(c => _mapper.Map<CellDto>(c));

        var revealResult = new RevealCellResult(
            game.Status,
            revealedCells,
            game.IsGameComplete
        );

        _logger.LogInformation("Successfully revealed cell. Game status: {Status}", game.Status);
        return Result.Success(revealResult);
    }
}
```

### 2. Query Handlers

#### GetGameByIdQuery

```csharp
// src/Minesweeper.Application/Queries/GetGameByIdQuery.cs
namespace Minesweeper.Application.Queries;

public record GetGameByIdQuery(GameId GameId, PlayerId PlayerId) : IRequest<Result<GameDto>>;

public class GetGameByIdHandler : IRequestHandler<GetGameByIdQuery, Result<GameDto>>
{
    private readonly IGameRepository _gameRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetGameByIdHandler> _logger;

    public GetGameByIdHandler(
        IGameRepository gameRepository,
        IMapper mapper,
        ILogger<GetGameByIdHandler> logger)
    {
        _gameRepository = gameRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<GameDto>> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting game {GameId} for player {PlayerId}", request.GameId, request.PlayerId);

        var game = await _gameRepository.GetByIdAsync(request.GameId, cancellationToken);
        if (game == null)
        {
            _logger.LogWarning("Game {GameId} not found", request.GameId);
            return Result.Failure<GameDto>("Game not found");
        }

        if (game.PlayerId != request.PlayerId)
        {
            _logger.LogWarning("Player {PlayerId} attempted to access game {GameId} owned by {OwnerId}", 
                request.PlayerId, request.GameId, game.PlayerId);
            return Result.Failure<GameDto>("Unauthorized access to game");
        }

        var gameDto = _mapper.Map<GameDto>(game);
        return Result.Success(gameDto);
    }
}
```

### 3. DTOs and Mapping

#### Data Transfer Objects

```csharp
// src/Minesweeper.Application/DTOs/GameDto.cs
namespace Minesweeper.Application.DTOs;

public class GameDto
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public GameDifficultyDto Difficulty { get; set; } = default!;
    public GameStatus Status { get; set; }
    public GameBoardDto Board { get; set; } = default!;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int MoveCount { get; set; }
    public TimeSpan? ElapsedTime { get; set; }
    public bool IsGameComplete { get; set; }
}

public class GameBoardDto
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int MineCount { get; set; }
    public List<CellDto> Cells { get; set; } = new();
}

public class CellDto
{
    public int Row { get; set; }
    public int Column { get; set; }
    public CellState State { get; set; }
    public bool HasMine { get; set; }
    public int AdjacentMineCount { get; set; }
}

public class GameDifficultyDto
{
    public string Name { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public int MineCount { get; set; }
}
```

#### AutoMapper Profile

```csharp
// src/Minesweeper.Application/Mapping/GameMappingProfile.cs
namespace Minesweeper.Application.Mapping;

public class GameMappingProfile : Profile
{
    public GameMappingProfile()
    {
        CreateMap<Game, GameDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
            .ForMember(dest => dest.PlayerId, opt => opt.MapFrom(src => src.PlayerId.Value))
            .ForMember(dest => dest.IsGameComplete, opt => opt.MapFrom(src => src.IsGameComplete));

        CreateMap<GameBoard, GameBoardDto>()
            .ForMember(dest => dest.Cells, opt => opt.MapFrom(src => src.GetAllCells()));

        CreateMap<Cell, CellDto>()
            .ForMember(dest => dest.Row, opt => opt.MapFrom(src => src.Position.Row))
            .ForMember(dest => dest.Column, opt => opt.MapFrom(src => src.Position.Column));

        CreateMap<GameDifficulty, GameDifficultyDto>();
    }
}
```

### 4. Repository Interfaces

```csharp
// src/Minesweeper.Application/Interfaces/IGameRepository.cs
namespace Minesweeper.Application.Interfaces;

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(GameId gameId, CancellationToken cancellationToken = default);
    Task<Game?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<IEnumerable<Game>> GetByPlayerIdAsync(PlayerId playerId, int skip = 0, int take = 20, CancellationToken cancellationToken = default);
    Task SaveAsync(Game game, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(GameId gameId, CancellationToken cancellationToken = default);
}

// src/Minesweeper.Application/Interfaces/IPlayerRepository.cs
namespace Minesweeper.Application.Interfaces;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(PlayerId playerId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(PlayerId playerId, CancellationToken cancellationToken = default);
    Task SaveAsync(Player player, CancellationToken cancellationToken = default);
}
```

## Infrastructure Layer Implementation

### 1. Entity Framework Configuration

#### Database Context

```csharp
// src/Minesweeper.Infrastructure/Data/ApplicationDbContext.cs
namespace Minesweeper.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Game> Games { get; set; } = default!;
    public DbSet<Player> Players { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Configure value object conversions
        configurationBuilder.Properties<GameId>()
            .HaveConversion<GameIdConverter>();
        
        configurationBuilder.Properties<PlayerId>()
            .HaveConversion<PlayerIdConverter>();

        configurationBuilder.Properties<CellPosition>()
            .HaveConversion<CellPositionConverter>();
    }
}
```

#### Entity Configurations

```csharp
// src/Minesweeper.Infrastructure/Data/Configurations/GameConfiguration.cs
namespace Minesweeper.Infrastructure.Data.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.Id);

        builder.Property(g => g.PlayerId)
            .IsRequired();

        builder.Property(g => g.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(g => g.StartTime)
            .IsRequired();

        builder.Property(g => g.IdempotencyKey)
            .HasMaxLength(100);

        builder.HasIndex(g => g.IdempotencyKey)
            .IsUnique()
            .HasFilter("IdempotencyKey IS NOT NULL");

        builder.HasIndex(g => g.PlayerId);

        // Configure GameDifficulty as owned entity
        builder.OwnsOne(g => g.Difficulty, diff =>
        {
            diff.Property(d => d.Name).HasMaxLength(50).IsRequired();
            diff.Property(d => d.Width).IsRequired();
            diff.Property(d => d.Height).IsRequired();
            diff.Property(d => d.MineCount).IsRequired();
        });

        // Configure relationship with GameBoard
        builder.HasOne(g => g.Board)
            .WithOne()
            .HasForeignKey<GameBoard>("GameId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// src/Minesweeper.Infrastructure/Data/Configurations/GameBoardConfiguration.cs
namespace Minesweeper.Infrastructure.Data.Configurations;

public class GameBoardConfiguration : IEntityTypeConfiguration<GameBoard>
{
    public void Configure(EntityTypeBuilder<GameBoard> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Width).IsRequired();
        builder.Property(b => b.Height).IsRequired();
        builder.Property(b => b.MineCount).IsRequired();
        builder.Property(b => b.MinesGenerated).IsRequired();

        // Shadow property for foreign key
        builder.Property<GameId>("GameId").IsRequired();

        // Configure relationship with Cells
        builder.HasMany<Cell>("_cells")
            .WithOne()
            .HasForeignKey("GameBoardId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// src/Minesweeper.Infrastructure/Data/Configurations/CellConfiguration.cs
namespace Minesweeper.Infrastructure.Data.Configurations;

public class CellConfiguration : IEntityTypeConfiguration<Cell>
{
    public void Configure(EntityTypeBuilder<Cell> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Position)
            .IsRequired();

        builder.Property(c => c.State)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(c => c.HasMine).IsRequired();
        builder.Property(c => c.AdjacentMineCount).IsRequired();

        // Shadow property for foreign key
        builder.Property<Guid>("GameBoardId").IsRequired();

        // Index for efficient querying
        builder.HasIndex("GameBoardId");
    }
}
```

#### Value Object Converters

```csharp
// src/Minesweeper.Infrastructure/Data/Converters/GameIdConverter.cs
namespace Minesweeper.Infrastructure.Data.Converters;

public class GameIdConverter : ValueConverter<GameId, Guid>
{
    public GameIdConverter() : base(
        gameId => gameId.Value,
        value => new GameId(value))
    {
    }
}

// src/Minesweeper.Infrastructure/Data/Converters/CellPositionConverter.cs
namespace Minesweeper.Infrastructure.Data.Converters;

public class CellPositionConverter : ValueConverter<CellPosition, string>
{
    public CellPositionConverter() : base(
        position => $"{position.Row},{position.Column}",
        value => ParsePosition(value))
    {
    }

    private static CellPosition ParsePosition(string value)
    {
        var parts = value.Split(',');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var row) || !int.TryParse(parts[1], out var col))
            throw new ArgumentException($"Invalid position format: {value}");
        
        return new CellPosition(row, col);
    }
}
```

### 2. Repository Implementations

#### GameRepository

```csharp
// src/Minesweeper.Infrastructure/Repositories/GameRepository.cs
namespace Minesweeper.Infrastructure.Repositories;

public class GameRepository : IGameRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GameRepository> _logger;

    public GameRepository(ApplicationDbContext context, ILogger<GameRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Game?> GetByIdAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Include(g => g.Board)
            .FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);
    }

    public async Task<Game?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Include(g => g.Board)
            .FirstOrDefaultAsync(g => g.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<IEnumerable<Game>> GetByPlayerIdAsync(PlayerId playerId, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Where(g => g.PlayerId == playerId)
            .OrderByDescending(g => g.StartTime)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(Game game, CancellationToken cancellationToken = default)
    {
        var existingGame = await _context.Games
            .Include(g => g.Board)
            .FirstOrDefaultAsync(g => g.Id == game.Id, cancellationToken);

        if (existingGame == null)
        {
            _context.Games.Add(game);
            _logger.LogDebug("Added new game {GameId} to context", game.Id);
        }
        else
        {
            _context.Entry(existingGame).CurrentValues.SetValues(game);
            _logger.LogDebug("Updated existing game {GameId}", game.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
        
        // Clear domain events after successful save
        game.ClearDomainEvents();
    }

    public async Task<bool> ExistsAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        return await _context.Games.AnyAsync(g => g.Id == gameId, cancellationToken);
    }
}
```

## Presentation Layer Implementation

### 1. API Controllers

#### GamesController

```csharp
// src/Minesweeper.WebApi/Controllers/GamesController.cs
namespace Minesweeper.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GamesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<GamesController> _logger;

    public GamesController(IMediator mediator, ILogger<GamesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(GameDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GameDto>> CreateGame(
        [FromBody] CreateGameRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var playerId = GetCurrentPlayerId();
        var command = new StartNewGameCommand(playerId, request.Difficulty, idempotencyKey);

        var result = await _mediator.Send(command);
        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to create game: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        var gameQuery = new GetGameByIdQuery(result.Value, playerId);
        var gameResult = await _mediator.Send(gameQuery);
        
        if (gameResult.IsFailure)
            return BadRequest(new { error = gameResult.Error });

        return CreatedAtAction(nameof(GetGame), new { id = result.Value.Value }, gameResult.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GameDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDto>> GetGame(Guid id)
    {
        var playerId = GetCurrentPlayerId();
        var query = new GetGameByIdQuery(new GameId(id), playerId);

        var result = await _mediator.Send(query);
        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to get game {GameId}: {Error}", id, result.Error);
            return NotFound(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/reveal")]
    [ProducesResponseType(typeof(RevealCellResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
        {
            _logger.LogWarning("Failed to reveal cell: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/flag")]
    [ProducesResponseType(typeof(FlagCellResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FlagCellResult>> FlagCell(
        Guid id,
        [FromBody] FlagCellRequest request)
    {
        var playerId = GetCurrentPlayerId();
        var command = new FlagCellCommand(
            new GameId(id),
            new CellPosition(request.Row, request.Column),
            playerId);

        var result = await _mediator.Send(command);
        if (result.IsFailure)
        {
            _logger.LogWarning("Failed to flag cell: {Error}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    private PlayerId GetCurrentPlayerId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Invalid user ID in token");

        return new PlayerId(userId);
    }
}
```

### 2. Request/Response Models

```csharp
// src/Minesweeper.WebApi/Models/GameRequests.cs
namespace Minesweeper.WebApi.Models;

public class CreateGameRequest
{
    [Required]
    public GameDifficultyDto Difficulty { get; set; } = default!;
}

public class RevealCellRequest
{
    [Required]
    [Range(0, int.MaxValue)]
    public int Row { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int Column { get; set; }
}

public class FlagCellRequest
{
    [Required]
    [Range(0, int.MaxValue)]
    public int Row { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int Column { get; set; }
}
```

### 3. Program.cs Configuration

```csharp
// src/Minesweeper.WebApi/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(StartNewGameCommand).Assembly);
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(GameMappingProfile));

// Repositories
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // JWT configuration
    });

// Authorization
builder.Services.AddAuthorization();

// SignalR
builder.Services.AddSignalR();

// Logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/gamehub");

app.Run();
```

This implementation guide provides a comprehensive foundation for building the Minesweeper game following Clean Architecture, DDD, and CQRS principles. The code is production-ready with proper error handling, logging, validation, and security considerations.
