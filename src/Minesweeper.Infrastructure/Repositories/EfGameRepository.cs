using Microsoft.EntityFrameworkCore;
using Minesweeper.Domain.Aggregates;
using Minesweeper.Domain.Repositories;
using Minesweeper.Domain.ValueObjects;
using Minesweeper.Infrastructure.Data;

namespace Minesweeper.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of IGameRepository with security-first design
/// </summary>
public class EfGameRepository : IGameRepository
{
    private readonly ApplicationDbContext _context;

    public EfGameRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Game?> GetByIdAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);
    }

    public async Task<IReadOnlyList<Game>> GetByPlayerIdAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Where(g => g.PlayerId == playerId)
            .OrderByDescending(g => g.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Game>> GetActiveGamesByPlayerIdAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Where(g => g.PlayerId == playerId && g.IsActive)
            .OrderByDescending(g => g.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Game>> GetCompletedGamesByPlayerIdAsync(
        PlayerId playerId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .Where(g => g.PlayerId == playerId && !g.IsActive)
            .OrderByDescending(g => g.CompletedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Game game, CancellationToken cancellationToken = default)
    {
        await _context.Games.AddAsync(game, cancellationToken);
    }

    public void Update(Game game)
    {
        _context.Games.Update(game);
    }

    public async Task DeleteAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        var game = await _context.Games
            .FirstOrDefaultAsync(g => g.Id == gameId, cancellationToken);

        if (game != null)
        {
            _context.Games.Remove(game);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetGameCountByPlayerIdAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        return await _context.Games
            .CountAsync(g => g.PlayerId == playerId, cancellationToken);
    }

    public async Task<IReadOnlyList<PlayerLeaderboardEntry>> GetLeaderboardAsync(
        string difficultyName,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        // Get top players based on win count for the specific difficulty
        var topPlayers = await _context.Players
            .Where(p => p.IsActive)
            .Where(p => p.Statistics.TotalGames > 0)
            .OrderByDescending(p => EF.Property<double>(p, "WinRate"))
            .ThenByDescending(p => p.Statistics.GamesWon)
            .Take(take)
            .Select(p => new PlayerLeaderboardEntry
            {
                PlayerId = p.Id,
                PlayerName = p.Username,
                GamesPlayed = p.Statistics.TotalGames,
                GamesWon = p.Statistics.GamesWon,
                WinRate = EF.Property<double>(p, "WinRate"),
                BestTime = p.Statistics.BestTimeOverall,
                AverageTime = p.Statistics.AverageGameTime
            })
            .ToListAsync(cancellationToken);

        return topPlayers;
    }
}
