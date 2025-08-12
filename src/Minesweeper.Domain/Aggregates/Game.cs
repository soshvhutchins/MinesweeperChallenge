using Minesweeper.Domain.Common;
using Minesweeper.Domain.Entities;
using Minesweeper.Domain.Enums;
using Minesweeper.Domain.Events;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.Domain.Aggregates;

/// <summary>
/// Game aggregate root representing a complete minesweeper game session
/// </summary>
public class Game : Entity<GameId>
{
    private readonly List<CellPosition> _revealedPositions = new();
    private DateTime? _pausedAt;

    public PlayerId PlayerId { get; private set; } = default!;
    public GameBoard Board { get; private set; } = default!;
    public GameStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public bool IsFirstMove { get; private set; }

    // EF Core navigation properties
    public string PlayerIdValue { get; private set; } = string.Empty;
    public string DifficultyName { get; private set; } = string.Empty;
    public int BoardRows { get; private set; }
    public int BoardColumns { get; private set; }
    public int MineCount { get; private set; }

    private Game() { } // For EF Core

    /// <summary>
    /// Reconstructs the board from persisted properties after EF Core loads the entity
    /// </summary>
    public void ReconstructBoard()
    {
        if (Board != null) return; // Already initialized

        var difficulty = GameDifficulty.FromName(DifficultyName);
        if (difficulty == null)
        {
            // Fallback: create custom difficulty from persisted properties
            var customResult = GameDifficulty.CreateCustom(DifficultyName, BoardRows, BoardColumns, MineCount);
            if (customResult.IsSuccess)
                difficulty = customResult.Value;
            else
                throw new InvalidOperationException($"Cannot reconstruct game difficulty: {customResult.Error}");
        }

        Board = new GameBoard(difficulty);
    }

    /// <summary>
    /// Ensures the board is initialized before use
    /// </summary>
    private void EnsureBoardInitialized()
    {
        if (Board == null)
            ReconstructBoard();
    }

    public Game(GameId gameId, PlayerId playerId, GameDifficulty difficulty, Random? random = null)
    {
        Id = gameId ?? throw new ArgumentNullException(nameof(gameId));
        PlayerId = playerId ?? throw new ArgumentNullException(nameof(playerId));
        Board = new GameBoard(difficulty, random);
        Status = GameStatus.NotStarted;
        IsFirstMove = true;

        // EF Core properties
        PlayerIdValue = playerId.Value.ToString();
        DifficultyName = difficulty.Name;
        BoardRows = difficulty.Rows;
        BoardColumns = difficulty.Columns;
        MineCount = difficulty.MineCount;
    }

    /// <summary>
    /// Starts the game and places mines, avoiding the first clicked position
    /// </summary>
    public Result StartGame(CellPosition firstClickPosition)
    {
        if (Status != GameStatus.NotStarted)
            return Result.Failure("Game has already been started");

        if (!Board.IsValidPosition(firstClickPosition))
            return Result.Failure("Invalid first click position");

        // Place mines avoiding the first click position
        var placeMinesResult = Board.PlaceMines(firstClickPosition);
        if (placeMinesResult.IsFailure)
            return placeMinesResult;

        StartedAt = DateTime.UtcNow;
        Status = GameStatus.InProgress;

        RaiseDomainEvent(new GameStartedEvent(Id, PlayerId, Board.Difficulty));

        // Automatically reveal the first cell
        return RevealCell(firstClickPosition);
    }

    /// <summary>
    /// Reveals a cell on the board
    /// </summary>
    public Result RevealCell(CellPosition position)
    {
        EnsureBoardInitialized();

        if (Status == GameStatus.Won || Status == GameStatus.Lost)
            return Result.Failure("Game is completed");

        if (!Board.IsValidPosition(position))
            return Result.Failure("Invalid position");

        // Handle first move
        if (IsFirstMove && Status == GameStatus.NotStarted)
        {
            var startResult = StartGame(position);
            if (startResult.IsFailure)
                return startResult;

            IsFirstMove = false;
            return Result.Success();
        }

        if (Status != GameStatus.InProgress)
            return Result.Failure("Game is not in progress");

        if (IsFirstMove)
        {
            IsFirstMove = false;
        }

        var revealResult = Board.RevealCell(position);
        if (revealResult.IsFailure)
            return Result.Failure(revealResult.Error);

        var revealedPositions = revealResult.Value;
        if (revealedPositions.Count == 0)
            return Result.Success(); // Cell was already revealed

        _revealedPositions.AddRange(revealedPositions);

        var cell = Board.GetCell(position);

        // Raise cell revealed event
        RaiseDomainEvent(new CellRevealedEvent(
            Id,
            PlayerId,
            position,
            cell.HasMine,
            cell.AdjacentMineCount,
            revealedPositions));

        // Check for mine hit (game lost)
        if (cell.HasMine)
        {
            Status = GameStatus.Lost;
            CompletedAt = DateTime.UtcNow;
            Board.RevealAllMines();

            RaiseDomainEvent(new GameLostEvent(Id, PlayerId, position, GetGameDuration()));
            return Result.Success();
        }

        // Check for win condition
        if (Board.AreAllSafeCellsRevealed())
        {
            Status = GameStatus.Won;
            CompletedAt = DateTime.UtcNow;

            RaiseDomainEvent(new GameWonEvent(Id, PlayerId, GetGameDuration(), Board.GetFlaggedCount()));
        }

        return Result.Success();
    }

    /// <summary>
    /// Toggles a flag on a cell
    /// </summary>
    public Result ToggleFlag(CellPosition position)
    {
        EnsureBoardInitialized();

        if (Status == GameStatus.Won || Status == GameStatus.Lost)
            return Result.Failure("Game is completed");

        var flagResult = Board.ToggleFlag(position);
        if (flagResult.IsFailure)
            return flagResult;

        var cell = Board.GetCell(position);
        RaiseDomainEvent(new CellFlaggedEvent(Id, PlayerId, position, cell.IsFlagged));

        return Result.Success();
    }

    /// <summary>
    /// Toggles a question mark on a cell
    /// </summary>
    public Result ToggleQuestion(CellPosition position)
    {
        EnsureBoardInitialized();

        if (Status != GameStatus.InProgress)
            return Result.Failure("Game is not in progress");

        return Board.ToggleQuestion(position);
    }

    /// <summary>
    /// Pauses the game
    /// </summary>
    public Result PauseGame()
    {
        if (Status != GameStatus.InProgress)
            return Result.Failure("Game is not in progress");

        Status = GameStatus.Paused;
        _pausedAt = DateTime.UtcNow;

        RaiseDomainEvent(new GamePausedEvent(Id, PlayerId));
        return Result.Success();
    }

    /// <summary>
    /// Resumes a paused game
    /// </summary>
    public Result ResumeGame()
    {
        if (Status != GameStatus.Paused)
            return Result.Failure("Game is not paused");

        Status = GameStatus.InProgress;

        // Adjust start time to account for pause duration
        if (_pausedAt.HasValue)
        {
            var pauseDuration = DateTime.UtcNow - _pausedAt.Value;
            StartedAt = StartedAt.Add(pauseDuration);
            _pausedAt = null;
        }

        RaiseDomainEvent(new GameResumedEvent(Id, PlayerId));
        return Result.Success();
    }

    /// <summary>
    /// Gets the current game duration
    /// </summary>
    public TimeSpan GetGameDuration()
    {
        if (Status == GameStatus.NotStarted)
            return TimeSpan.Zero;

        var endTime = CompletedAt ?? (_pausedAt ?? DateTime.UtcNow);
        return endTime - StartedAt;
    }

    /// <summary>
    /// Gets the number of remaining mines (mine count - flags)
    /// </summary>
    public int GetRemainingMineCount()
    {
        EnsureBoardInitialized();
        return Math.Max(0, Board.Difficulty.MineCount - Board.GetFlaggedCount());
    }

    /// <summary>
    /// Gets the game progress percentage
    /// </summary>
    public double GetProgressPercentage()
    {
        EnsureBoardInitialized();

        if (!Board.IsInitialized)
            return 0.0;

        var totalSafeCells = Board.Difficulty.SafeCells;
        var revealedSafeCells = Board.GetAllCells().Count(c => c.IsRevealed && !c.HasMine);

        return totalSafeCells == 0 ? 100.0 : (double)revealedSafeCells / totalSafeCells * 100.0;
    }

    /// <summary>
    /// Checks if the game is completed (won or lost)
    /// </summary>
    public bool IsCompleted => Status == GameStatus.Won || Status == GameStatus.Lost;

    /// <summary>
    /// Checks if the game is active (in progress)
    /// </summary>
    public bool IsActive => Status == GameStatus.InProgress;

    /// <summary>
    /// Gets game statistics
    /// </summary>
    public GameStatistics GetStatistics()
    {
        EnsureBoardInitialized();

        return new GameStatistics
        {
            GameId = Id,
            PlayerId = PlayerId,
            Difficulty = Board.Difficulty,
            Status = Status,
            Duration = GetGameDuration(),
            CellsRevealed = Board.GetRevealedCount(),
            FlagsUsed = Board.GetFlaggedCount(),
            ProgressPercentage = GetProgressPercentage(),
            StartedAt = StartedAt,
            CompletedAt = CompletedAt
        };
    }
}

/// <summary>
/// Game statistics value object
/// </summary>
public record GameStatistics
{
    public GameId GameId { get; init; } = default!;
    public PlayerId PlayerId { get; init; } = default!;
    public GameDifficulty Difficulty { get; init; } = default!;
    public GameStatus Status { get; init; }
    public TimeSpan Duration { get; init; }
    public int CellsRevealed { get; init; }
    public int FlagsUsed { get; init; }
    public double ProgressPercentage { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
