using Minesweeper.Domain.Common;

namespace Minesweeper.Domain.ValueObjects;

/// <summary>
/// Represents the game difficulty levels with associated board configuration
/// </summary>
public record GameDifficulty
{
    public string Name { get; }
    public int Rows { get; }
    public int Columns { get; }
    public int MineCount { get; }

    private GameDifficulty(string name, int rows, int columns, int mineCount)
    {
        Name = name;
        Rows = rows;
        Columns = columns;
        MineCount = mineCount;
    }

    // Predefined difficulty levels
    public static readonly GameDifficulty Beginner = new("Beginner", 9, 9, 10);
    public static readonly GameDifficulty Intermediate = new("Intermediate", 16, 16, 40);
    public static readonly GameDifficulty Expert = new("Expert", 16, 30, 99);

    /// <summary>
    /// Creates a custom difficulty level
    /// </summary>
    public static Result<GameDifficulty> CreateCustom(string name, int rows, int columns, int mineCount)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<GameDifficulty>("Difficulty name cannot be empty");

        if (rows <= 0)
            return Result.Failure<GameDifficulty>("Rows must be greater than zero");

        if (columns <= 0)
            return Result.Failure<GameDifficulty>("Columns must be greater than zero");

        if (mineCount <= 0)
            return Result.Failure<GameDifficulty>("Mine count must be greater than zero");

        var totalCells = rows * columns;

        // Ensure at least one safe cell
        if (mineCount >= totalCells)
            return Result.Failure<GameDifficulty>("Must have at least one safe cell");

        return Result.Success(new GameDifficulty(name, rows, columns, mineCount));
    }

    /// <summary>
    /// Gets difficulty by name (case-insensitive)
    /// </summary>
    public static GameDifficulty? FromName(string name)
    {
        return name?.ToLowerInvariant() switch
        {
            "beginner" => Beginner,
            "intermediate" => Intermediate,
            "expert" => Expert,
            _ => null
        };
    }

    /// <summary>
    /// Gets all predefined difficulties
    /// </summary>
    public static IReadOnlyList<GameDifficulty> GetPredefinedDifficulties()
    {
        return new[] { Beginner, Intermediate, Expert };
    }

    /// <summary>
    /// Calculates the total number of cells
    /// </summary>
    public int TotalCells => Rows * Columns;

    /// <summary>
    /// Calculates the number of safe (non-mine) cells
    /// </summary>
    public int SafeCells => TotalCells - MineCount;

    /// <summary>
    /// Calculates the mine density percentage
    /// </summary>
    public double MineDensity => (double)MineCount / TotalCells * 100;

    public override string ToString() => $"{Name} ({Rows}x{Columns}, {MineCount} mines)";
}
