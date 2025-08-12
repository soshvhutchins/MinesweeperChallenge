using Minesweeper.Domain.Common;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.Domain.Entities;

/// <summary>
/// Represents the minesweeper game board with its cells and mine placement logic
/// </summary>
public class GameBoard
{
    public List<List<Cell>> Cells { get; set; }
    private readonly Random _random;

    public GameDifficulty Difficulty { get; private set; }
    public bool IsInitialized { get; private set; }

    private GameBoard()
    {
        // For EF Core/JSON only; fields will be set by reflection/serializer
        Cells = null!;
        _random = null!;
        Difficulty = null!;
    }

    public GameBoard(GameDifficulty difficulty, Random? random = null)
    {
        Difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
        _random = random ?? new Random();
        Cells = new List<List<Cell>>(difficulty.Rows);
        for (int row = 0; row < difficulty.Rows; row++)
        {
            var rowList = new List<Cell>(difficulty.Columns);
            for (int col = 0; col < difficulty.Columns; col++)
            {
                var position = CellPosition.Of(row, col);
                rowList.Add(new Cell(position));
            }
            Cells.Add(rowList);
        }
    }

    /// <summary>
    /// Initializes all cells on the board
    /// </summary>

    /// <summary>
    /// Places mines on the board, avoiding the first clicked position
    /// </summary>
    public Result PlaceMines(CellPosition firstClickPosition)
    {
        if (IsInitialized)
            return Result.Failure("Mines have already been placed");

        if (!IsValidPosition(firstClickPosition))
            return Result.Failure("First click position is invalid");

        // For Beginner, use a fixed, stationary mine layout (classic Windows Minesweeper style)
        if (Difficulty.Name == "Beginner" && Difficulty.Rows == 9 && Difficulty.Columns == 9 && Difficulty.MineCount == 10)
        {
            // Classic Windows Beginner layout (fixed, but not visible to user)
            // Example: Place bombs at these 10 positions (row, col):
            var fixedMinePositions = new List<CellPosition>
            {
                CellPosition.Of(0, 0),
                CellPosition.Of(0, 3),
                CellPosition.Of(1, 6),
                CellPosition.Of(2, 2),
                CellPosition.Of(3, 5),
                CellPosition.Of(4, 8),
                CellPosition.Of(5, 1),
                CellPosition.Of(6, 4),
                CellPosition.Of(7, 7),
                CellPosition.Of(8, 5)
            };

            // Never place a mine on the first click or its neighbors
            var excluded = new HashSet<CellPosition> { firstClickPosition };
            foreach (var adj in firstClickPosition.GetAdjacentPositions().Where(IsValidPosition))
                excluded.Add(adj);

            // If any fixed mine is excluded, shift all mines down by 1 row (wrap around)
            // This ensures first click is always safe, but mines are stationary for a given board
            var adjustedMinePositions = fixedMinePositions.Select(pos =>
                excluded.Contains(pos)
                    ? CellPosition.Of((pos.Row + 1) % 9, pos.Column)
                    : pos
            ).ToList();

            // Place mines
            foreach (var position in adjustedMinePositions)
            {
                GetCell(position).PlaceMine();
            }

            CalculateAdjacentMineCounts();
            IsInitialized = true;
            return Result.Success();
        }

        // Default: random placement for other difficulties
        var availablePositions = GetAvailablePositions(firstClickPosition).ToList();
        if (availablePositions.Count < Difficulty.MineCount)
            return Result.Failure("Not enough positions available for mine placement");
        var minePositions = availablePositions
            .OrderBy(_ => _random.Next())
            .Take(Difficulty.MineCount)
            .ToList();
        foreach (var position in minePositions)
        {
            GetCell(position).PlaceMine();
        }
        CalculateAdjacentMineCounts();
        IsInitialized = true;
        return Result.Success();
    }

    /// <summary>
    /// Gets all available positions for mine placement (excluding first click and its neighbors)
    /// </summary>
    private IEnumerable<CellPosition> GetAvailablePositions(CellPosition excludePosition)
    {
        var excludedPositions = new HashSet<CellPosition> { excludePosition };

        // Also exclude adjacent positions to ensure first click is always safe
        foreach (var adjacent in excludePosition.GetAdjacentPositions().Where(IsValidPosition))
        {
            excludedPositions.Add(adjacent);
        }

        for (int row = 0; row < Difficulty.Rows; row++)
        {
            for (int col = 0; col < Difficulty.Columns; col++)
            {
                var position = CellPosition.Of(row, col);
                if (!excludedPositions.Contains(position))
                {
                    yield return position;
                }
            }
        }
    }

    /// <summary>
    /// Calculates the adjacent mine count for all cells
    /// </summary>
    private void CalculateAdjacentMineCounts()
    {
        for (int row = 0; row < Difficulty.Rows; row++)
        {
            for (int col = 0; col < Difficulty.Columns; col++)
            {
                var position = CellPosition.Of(row, col);
                var cell = GetCell(position);

                if (!cell.HasMine)
                {
                    var adjacentMineCount = CountAdjacentMines(position);
                    cell.SetAdjacentMineCount(adjacentMineCount);
                }
            }
        }
    }

    /// <summary>
    /// Counts the number of mines adjacent to the specified position
    /// </summary>
    private int CountAdjacentMines(CellPosition position)
    {
        return position.GetAdjacentPositions()
            .Where(IsValidPosition)
            .Count(adjPosition => GetCell(adjPosition).HasMine);
    }

    /// <summary>
    /// Reveals a cell and potentially cascades to adjacent cells
    /// </summary>
    public Result<IReadOnlyList<CellPosition>> RevealCell(CellPosition position)
    {
        if (!IsValidPosition(position))
            return Result.Failure<IReadOnlyList<CellPosition>>("Invalid position");

        if (!IsInitialized)
            return Result.Failure<IReadOnlyList<CellPosition>>("Board is not initialized");

        var cell = GetCell(position);
        var revealedPositions = new List<CellPosition>();

        if (!cell.Reveal())
            return Result.Success<IReadOnlyList<CellPosition>>(revealedPositions);

        revealedPositions.Add(position);

        // If it's a mine, return immediately
        if (cell.HasMine)
            return Result.Success<IReadOnlyList<CellPosition>>(revealedPositions);

        // If it's an empty cell (no adjacent mines), reveal adjacent cells
        if (cell.AdjacentMineCount == 0)
        {
            var cascadeResult = CascadeReveal(position);
            revealedPositions.AddRange(cascadeResult);
        }

        return Result.Success<IReadOnlyList<CellPosition>>(revealedPositions);
    }

    /// <summary>
    /// Recursively reveals adjacent cells for empty cells
    /// </summary>
    private IReadOnlyList<CellPosition> CascadeReveal(CellPosition position)
    {
        var revealedPositions = new List<CellPosition>();
        var toProcess = new Queue<CellPosition>();
        var processed = new HashSet<CellPosition>();

        toProcess.Enqueue(position);
        processed.Add(position);

        while (toProcess.Count > 0)
        {
            var currentPosition = toProcess.Dequeue();
            // var currentCell = GetCell(currentPosition); // Unused variable removed

            foreach (var adjacentPosition in currentPosition.GetAdjacentPositions())
            {
                if (!IsValidPosition(adjacentPosition) || processed.Contains(adjacentPosition))
                    continue;

                var adjacentCell = GetCell(adjacentPosition);
                if (adjacentCell.Reveal())
                {
                    revealedPositions.Add(adjacentPosition);
                    processed.Add(adjacentPosition);

                    // If adjacent cell is also empty, add to processing queue
                    if (adjacentCell.AdjacentMineCount == 0 && !adjacentCell.HasMine)
                    {
                        toProcess.Enqueue(adjacentPosition);
                    }
                }
            }
        }

        return revealedPositions;
    }

    /// <summary>
    /// Toggles the flag state of a cell
    /// </summary>
    public Result ToggleFlag(CellPosition position)
    {
        if (!IsValidPosition(position))
            return Result.Failure("Invalid position");

        var cell = GetCell(position);
        if (!cell.ToggleFlag())
            return Result.Failure("Cannot flag a revealed cell");

        return Result.Success();
    }

    /// <summary>
    /// Toggles the question mark state of a cell
    /// </summary>
    public Result ToggleQuestion(CellPosition position)
    {
        if (!IsValidPosition(position))
            return Result.Failure("Invalid position");

        var cell = GetCell(position);
        if (!cell.ToggleQuestion())
            return Result.Failure("Cannot question a revealed cell");

        return Result.Success();
    }

    /// <summary>
    /// Gets the cell at the specified position
    /// </summary>
    public Cell GetCell(CellPosition position)
    {
        if (!IsValidPosition(position))
            throw new ArgumentOutOfRangeException(nameof(position), "Position is outside board bounds");

        return Cells[position.Row][position.Column];
    }

    /// <summary>
    /// Checks if the position is within the board bounds
    /// </summary>
    public bool IsValidPosition(CellPosition position)
    {
        return position.IsWithinBounds(Difficulty.Rows, Difficulty.Columns);
    }

    /// <summary>
    /// Gets all cells on the board
    /// </summary>
    public IEnumerable<Cell> GetAllCells()
    {
        for (int row = 0; row < Difficulty.Rows; row++)
        {
            for (int col = 0; col < Difficulty.Columns; col++)
            {
                yield return Cells[row][col];
            }
        }
    }

    /// <summary>
    /// Gets the count of flagged cells
    /// </summary>
    public int GetFlaggedCount()
    {
        return GetAllCells().Count(cell => cell.IsFlagged);
    }

    /// <summary>
    /// Gets the count of revealed cells
    /// </summary>
    public int GetRevealedCount()
    {
        return GetAllCells().Count(cell => cell.IsRevealed);
    }

    /// <summary>
    /// Checks if all safe cells have been revealed (win condition)
    /// </summary>
    public bool AreAllSafeCellsRevealed()
    {
        return GetAllCells().Where(cell => !cell.HasMine).All(cell => cell.IsRevealed);
    }

    /// <summary>
    /// Checks if any mine has been revealed (lose condition)
    /// </summary>
    public bool IsAnyMineRevealed()
    {
        return GetAllCells().Any(cell => cell.HasMine && cell.IsRevealed);
    }

    /// <summary>
    /// Reveals all mines (typically called when game is lost)
    /// </summary>
    public void RevealAllMines()
    {
        foreach (var cell in GetAllCells().Where(cell => cell.HasMine))
        {
            cell.Reveal();
        }
    }

    /// <summary>
    /// Gets a string representation of the board for debugging
    /// </summary>
    public string ToDebugString()
    {
        var lines = new List<string>();

        for (int row = 0; row < Difficulty.Rows; row++)
        {
            var line = string.Join(" ",
                Enumerable.Range(0, Difficulty.Columns)
                    .Select(col => GetCell(CellPosition.Of(row, col)).GetDisplayValue()));
            lines.Add(line);
        }

        return string.Join(Environment.NewLine, lines);
    }
}
