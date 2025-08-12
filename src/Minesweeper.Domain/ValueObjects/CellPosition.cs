using Minesweeper.Domain.Common;

namespace Minesweeper.Domain.ValueObjects;

/// <summary>
/// Represents a position on the game board (zero-indexed)
/// </summary>
public record CellPosition
{
    public int Row { get; }
    public int Column { get; }

    private CellPosition(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public static Result<CellPosition> Create(int row, int column)
    {
        if (row < 0)
            return Result.Failure<CellPosition>("Row cannot be negative");

        if (column < 0)
            return Result.Failure<CellPosition>("Column cannot be negative");

        return Result.Success(new CellPosition(row, column));
    }

    public static CellPosition Of(int row, int column)
    {
        var result = Create(row, column);
        if (result.IsFailure)
            throw new ArgumentException(result.Error);

        return result.Value;
    }

    /// <summary>
    /// Gets all adjacent positions (including diagonals)
    /// </summary>
    public IEnumerable<CellPosition> GetAdjacentPositions()
    {
        var adjacentDeltas = new[]
        {
            (-1, -1), (-1, 0), (-1, 1),
            ( 0, -1),          ( 0, 1),
            ( 1, -1), ( 1, 0), ( 1, 1)
        };

        foreach (var (rowDelta, colDelta) in adjacentDeltas)
        {
            var newRow = Row + rowDelta;
            var newCol = Column + colDelta;

            if (newRow >= 0 && newCol >= 0)
            {
                yield return new CellPosition(newRow, newCol);
            }
        }
    }

    /// <summary>
    /// Checks if this position is within the bounds of a board
    /// </summary>
    public bool IsWithinBounds(int maxRow, int maxColumn)
    {
        return Row >= 0 && Row < maxRow && Column >= 0 && Column < maxColumn;
    }

    public override string ToString() => $"({Row},{Column})";
}
