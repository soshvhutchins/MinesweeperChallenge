namespace Minesweeper.Domain.Enums;

/// <summary>
/// Represents the current state of a game
/// </summary>
public enum GameStatus
{
    /// <summary>
    /// Game has been created but not started
    /// </summary>
    NotStarted,

    /// <summary>
    /// Game is currently in progress
    /// </summary>
    InProgress,

    /// <summary>
    /// Game has been won by revealing all safe cells
    /// </summary>
    Won,

    /// <summary>
    /// Game has been lost by revealing a mine
    /// </summary>
    Lost,

    /// <summary>
    /// Game has been paused
    /// </summary>
    Paused
}
