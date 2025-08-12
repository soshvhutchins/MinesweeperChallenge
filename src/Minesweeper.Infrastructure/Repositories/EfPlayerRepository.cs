using Microsoft.EntityFrameworkCore;
using Minesweeper.Domain.Aggregates;
using Minesweeper.Domain.Repositories;
using Minesweeper.Domain.ValueObjects;
using Minesweeper.Infrastructure.Data;

namespace Minesweeper.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of IPlayerRepository with security-first design
/// </summary>
public class EfPlayerRepository : IPlayerRepository
{
    private readonly ApplicationDbContext _context;

    public EfPlayerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Player?> GetByIdAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        return await _context.Players
            .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);
    }

    public async Task<Player?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Players
            .FirstOrDefaultAsync(p => p.Username == username, cancellationToken);
    }

    public async Task<Player?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Players
            .FirstOrDefaultAsync(p => p.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        return await _context.Players
            .AnyAsync(p => p.Id == playerId, cancellationToken);
    }

    public async Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Players
            .AnyAsync(p => p.Username == username, cancellationToken);
    }

    public async Task<bool> IsEmailTakenAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Players
            .AnyAsync(p => p.Email == email, cancellationToken);
    }

    public async Task AddAsync(Player player, CancellationToken cancellationToken = default)
    {
        await _context.Players.AddAsync(player, cancellationToken);
    }

    public void Update(Player player)
    {
        _context.Players.Update(player);
    }

    public async Task DeleteAsync(PlayerId playerId, CancellationToken cancellationToken = default)
    {
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);

        if (player != null)
        {
            _context.Players.Remove(player);
        }
    }

    public async Task<IReadOnlyList<Player>> GetTopPlayersByWinRateAsync(
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.Players
            .Where(p => p.IsActive && p.Statistics.TotalGames >= 10) // Minimum 10 games for fair comparison
            .OrderByDescending(p => EF.Property<double>(p, "WinRate"))
            .ThenByDescending(p => p.Statistics.TotalGames)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Player>> GetTopPlayersByGamesWonAsync(
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.Players
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Statistics.GamesWon)
            .ThenByDescending(p => EF.Property<double>(p, "WinRate"))
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
