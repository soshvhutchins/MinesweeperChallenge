using Minesweeper.Domain.Enums;

namespace Minesweeper.Application.DTOs;

public record GameDto
{
    public Guid Id { get; init; }
    public Guid PlayerId { get; init; }
    public string DifficultyName { get; init; } = string.Empty;
    public int Rows { get; init; }
    public int Columns { get; init; }
    public int MineCount { get; init; }
    public GameStatus Status { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public TimeSpan Duration { get; init; }
    public int RemainingMines { get; init; }
    public double ProgressPercentage { get; init; }
    public bool IsFirstMove { get; init; }
    public BoardDto Board { get; init; } = default!;
}

public record BoardDto
{
    public int Rows { get; init; }
    public int Columns { get; init; }
    public CellDto[][] Cells { get; init; } = default!; // Changed from 2D array to jagged array
    public bool IsInitialized { get; init; }
}

public record CellDto
{
    public int Row { get; init; }
    public int Column { get; init; }
    public CellState State { get; init; }
    public bool HasMine { get; init; }
    public int AdjacentMineCount { get; init; }
    public string DisplayValue { get; init; } = string.Empty;
    public bool IsRevealed { get; init; }
    public bool IsFlagged { get; init; }
    public bool IsHidden { get; init; }
}

public record GameStatisticsDto
{
    public Guid GameId { get; init; }
    public Guid PlayerId { get; init; }
    public string DifficultyName { get; init; } = string.Empty;
    public GameStatus Status { get; init; }
    public TimeSpan Duration { get; init; }
    public int CellsRevealed { get; init; }
    public int FlagsUsed { get; init; }
    public double ProgressPercentage { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record PlayerStatisticsDto
{
    public Guid PlayerId { get; init; }
    public string PlayerName { get; init; } = string.Empty;
    public int GamesWon { get; init; }
    public int GamesPlayed { get; init; }
    public TimeSpan BestTime { get; init; }
    public TimeSpan AverageTime { get; init; }
    public double WinRate { get; init; }
}

public record GameDifficultyDto
{
    public string Name { get; init; } = string.Empty;
    public int Rows { get; init; }
    public int Columns { get; init; }
    public int MineCount { get; init; }
    public int TotalCells { get; init; }
    public int SafeCells { get; init; }
    public double MineDensity { get; init; }
}
