using Minesweeper.Domain.Common;
using Minesweeper.Domain.ValueObjects;

namespace Minesweeper.Domain.Aggregates;

/// <summary>
/// Player aggregate root representing a game player
/// </summary>
public class Player : Entity<PlayerId>
{
    private Player() { } // EF Core constructor

    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime LastLoginAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public PlayerStatistics Statistics { get; private set; } = default!;

    public static Player Create(PlayerId playerId, string username, string email, string passwordHash)
    {
        var player = new Player
        {
            Id = playerId,
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = true,
            Statistics = PlayerStatistics.Initialize()
        };

        return player;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void UpdateStatistics(TimeSpan gameTime, bool won, string difficultyName)
    {
        Statistics = Statistics.UpdateWith(gameTime, won, difficultyName);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}

/// <summary>
/// Player statistics value object
/// </summary>
public record PlayerStatistics
{
    public int TotalGames { get; init; }
    public int GamesWon { get; init; }
    public int GamesLost { get; init; }
    public TimeSpan TotalPlayTime { get; init; }
    public TimeSpan BestTimeOverall { get; init; }
    public Dictionary<string, DifficultyStatistics> DifficultyStats { get; init; } = new();

    public double WinRate => TotalGames > 0 ? (double)GamesWon / TotalGames * 100 : 0;
    public TimeSpan AverageGameTime => TotalGames > 0 ? TimeSpan.FromTicks(TotalPlayTime.Ticks / TotalGames) : TimeSpan.Zero;

    public static PlayerStatistics Initialize()
    {
        return new PlayerStatistics
        {
            TotalGames = 0,
            GamesWon = 0,
            GamesLost = 0,
            TotalPlayTime = TimeSpan.Zero,
            BestTimeOverall = TimeSpan.MaxValue,
            DifficultyStats = new Dictionary<string, DifficultyStatistics>()
        };
    }

    public PlayerStatistics UpdateWith(TimeSpan gameTime, bool won, string difficultyName)
    {
        var newDifficultyStats = DifficultyStats.ContainsKey(difficultyName)
            ? DifficultyStats[difficultyName].UpdateWith(gameTime, won)
            : DifficultyStatistics.Initialize().UpdateWith(gameTime, won);

        var updatedDifficultyStats = new Dictionary<string, DifficultyStatistics>(DifficultyStats)
        {
            [difficultyName] = newDifficultyStats
        };

        return this with
        {
            TotalGames = TotalGames + 1,
            GamesWon = won ? GamesWon + 1 : GamesWon,
            GamesLost = won ? GamesLost : GamesLost + 1,
            TotalPlayTime = TotalPlayTime.Add(gameTime),
            BestTimeOverall = won && gameTime < BestTimeOverall ? gameTime : BestTimeOverall,
            DifficultyStats = updatedDifficultyStats
        };
    }
}

/// <summary>
/// Statistics for a specific difficulty level
/// </summary>
public record DifficultyStatistics
{
    public int GamesPlayed { get; init; }
    public int GamesWon { get; init; }
    public TimeSpan BestTime { get; init; }
    public TimeSpan TotalTime { get; init; }

    public double WinRate => GamesPlayed > 0 ? (double)GamesWon / GamesPlayed * 100 : 0;
    public TimeSpan AverageTime => GamesPlayed > 0 ? TimeSpan.FromTicks(TotalTime.Ticks / GamesPlayed) : TimeSpan.Zero;

    public static DifficultyStatistics Initialize()
    {
        return new DifficultyStatistics
        {
            GamesPlayed = 0,
            GamesWon = 0,
            BestTime = TimeSpan.MaxValue,
            TotalTime = TimeSpan.Zero
        };
    }

    public DifficultyStatistics UpdateWith(TimeSpan gameTime, bool won)
    {
        return this with
        {
            GamesPlayed = GamesPlayed + 1,
            GamesWon = won ? GamesWon + 1 : GamesWon,
            BestTime = won && gameTime < BestTime ? gameTime : BestTime,
            TotalTime = TotalTime.Add(gameTime)
        };
    }
}
