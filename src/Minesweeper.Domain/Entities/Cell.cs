using Minesweeper.Domain.Enums;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.Domain.Entities;

/// <summary>
/// Represents a single cell on the minesweeper board
/// </summary>
public class Cell
{
    public CellPosition Position { get; private set; }
    public bool HasMine { get; private set; }
    public CellState State { get; private set; }
    public int AdjacentMineCount { get; private set; }

    private Cell() { } // For EF Core

    public Cell(CellPosition position)
    {
        Position = position ?? throw new ArgumentNullException(nameof(position));
        HasMine = false;
        State = CellState.Hidden;
        AdjacentMineCount = 0;
    }

    /// <summary>
    /// Places a mine in this cell
    /// </summary>
    public void PlaceMine()
    {
        if (State != CellState.Hidden)
            throw new InvalidOperationException("Cannot place mine in a revealed cell");

        HasMine = true;
    }

    /// <summary>
    /// Sets the count of adjacent mines
    /// </summary>
    public void SetAdjacentMineCount(int count)
    {
        if (count < 0 || count > 8)
            throw new ArgumentOutOfRangeException(nameof(count), "Adjacent mine count must be between 0 and 8");

        AdjacentMineCount = count;
    }

    /// <summary>
    /// Reveals the cell if it's currently hidden
    /// </summary>
    /// <returns>True if the cell was successfully revealed, false if it was already revealed or flagged</returns>
    public bool Reveal()
    {
        if (State != CellState.Hidden && State != CellState.Questioned)
            return false;

        State = CellState.Revealed;
        return true;
    }

    /// <summary>
    /// Toggles the flag state of the cell
    /// </summary>
    /// <returns>True if the cell was flagged/unflagged, false if the cell is revealed</returns>
    public bool ToggleFlag()
    {
        if (State == CellState.Revealed)
            return false;

        State = State switch
        {
            CellState.Hidden => CellState.Flagged,
            CellState.Flagged => CellState.Hidden,
            CellState.Questioned => CellState.Flagged,
            _ => State
        };

        return true;
    }

    /// <summary>
    /// Toggles the question mark state of the cell
    /// </summary>
    /// <returns>True if the cell was questioned/unquestioned, false if the cell is revealed</returns>
    public bool ToggleQuestion()
    {
        if (State == CellState.Revealed)
            return false;

        State = State switch
        {
            CellState.Hidden => CellState.Questioned,
            CellState.Questioned => CellState.Hidden,
            CellState.Flagged => CellState.Questioned,
            _ => State
        };

        return true;
    }

    /// <summary>
    /// Checks if this cell is safe to reveal (not a mine)
    /// </summary>
    public bool IsSafe => !HasMine;

    /// <summary>
    /// Checks if this cell is revealed
    /// </summary>
    public bool IsRevealed => State == CellState.Revealed;

    /// <summary>
    /// Checks if this cell is flagged
    /// </summary>
    public bool IsFlagged => State == CellState.Flagged;

    /// <summary>
    /// Checks if this cell is questioned
    /// </summary>
    public bool IsQuestioned => State == CellState.Questioned;

    /// <summary>
    /// Checks if this cell is hidden (not revealed, flagged, or questioned)
    /// </summary>
    public bool IsHidden => State == CellState.Hidden;

    /// <summary>
    /// Gets the display value for this cell when revealed
    /// </summary>
    public string GetDisplayValue()
    {
        return State switch
        {
            CellState.Revealed when HasMine => "💣",
            CellState.Revealed when AdjacentMineCount == 0 => " ",
            CellState.Revealed => AdjacentMineCount.ToString(),
            CellState.Flagged => "🚩",
            CellState.Questioned => "❓",
            _ => "■"
        };
    }

    public override string ToString()
    {
        return $"Cell at {Position}: {State}" + (HasMine ? " (Mine)" : $" (Adjacent: {AdjacentMineCount})");
    }
}
