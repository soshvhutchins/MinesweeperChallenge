using Minesweeper.Domain.Aggregates;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.Domain.Repositories;

/// <summary>
/// Repository interface for Game aggregate
/// </summary>
public interface IGameRepository
{
    /// <summary>
    /// Gets a game by its ID
    /// </summary>
    Task<Game?> GetByIdAsync(GameId gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all games for a specific player
    /// </summary>
    Task<IReadOnlyList<Game>> GetByPlayerIdAsync(PlayerId playerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active games for a specific player
    /// </summary>
    Task<IReadOnlyList<Game>> GetActiveGamesByPlayerIdAsync(PlayerId playerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets completed games for a specific player with pagination
    /// </summary>
    Task<IReadOnlyList<Game>> GetCompletedGamesByPlayerIdAsync(
        PlayerId playerId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new game
    /// </summary>
    Task AddAsync(Game game, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing game
    /// </summary>
    void Update(Game game);

    /// <summary>
    /// Deletes a game
    /// </summary>
    Task DeleteAsync(GameId gameId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets game count for a player
    /// </summary>
    Task<int> GetGameCountByPlayerIdAsync(PlayerId playerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets leaderboard data (top players by win count, completion time, etc.)
    /// </summary>
    Task<IReadOnlyList<PlayerLeaderboardEntry>> GetLeaderboardAsync(
        string difficultyName,
        int take = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Player statistics for leaderboard
/// </summary>
public record PlayerLeaderboardEntry
{
    public PlayerId PlayerId { get; init; } = default!;
    public string PlayerName { get; init; } = string.Empty;
    public int GamesWon { get; init; }
    public int GamesPlayed { get; init; }
    public TimeSpan BestTime { get; init; }
    public TimeSpan AverageTime { get; init; }
    public double WinRate { get; init; }
}
