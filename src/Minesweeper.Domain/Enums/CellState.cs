namespace Minesweeper.Domain.Enums;

/// <summary>
/// Represents the state of an individual cell
/// </summary>
public enum CellState
{
    /// <summary>
    /// Cell is hidden and not revealed
    /// </summary>
    Hidden,

    /// <summary>
    /// Cell has been revealed
    /// </summary>
    Revealed,

    /// <summary>
    /// Cell has been flagged as potentially containing a mine
    /// </summary>
    Flagged,

    /// <summary>
    /// Cell has been marked with a question mark (uncertain)
    /// </summary>
    Questioned
}
