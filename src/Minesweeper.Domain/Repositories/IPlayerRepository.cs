using Minesweeper.Domain.Aggregates;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.Domain.Repositories;

/// <summary>
/// Repository interface for Player aggregate
/// </summary>
public interface IPlayerRepository
{
    /// <summary>
    /// Gets a player by their ID
    /// </summary>
    Task<Player?> GetByIdAsync(PlayerId playerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a player by their username
    /// </summary>
    Task<Player?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a player by their email
    /// </summary>
    Task<Player?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a player exists by ID
    /// </summary>
    Task<bool> ExistsAsync(PlayerId playerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a username is already taken
    /// </summary>
    Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is already registered
    /// </summary>
    Task<bool> IsEmailTakenAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new player
    /// </summary>
    Task AddAsync(Player player, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing player
    /// </summary>
    void Update(Player player);

    /// <summary>
    /// Deletes a player
    /// </summary>
    Task DeleteAsync(PlayerId playerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top players by win rate for leaderboard
    /// </summary>
    Task<IReadOnlyList<Player>> GetTopPlayersByWinRateAsync(
        int take = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top players by total games won
    /// </summary>
    Task<IReadOnlyList<Player>> GetTopPlayersByGamesWonAsync(
        int take = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
