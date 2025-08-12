using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Minesweeper.Domain.Aggregates;
using System.Text.Json;

namespace Minesweeper.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Player aggregate
/// </summary>
public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.LastLoginAt)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Configure PlayerStatistics as owned entity
        builder.OwnsOne(p => p.Statistics, stats =>
        {
            stats.Property(s => s.TotalGames)
                .IsRequired()
                .HasDefaultValue(0)
                .HasColumnName("StatsTotalGames");

            stats.Property(s => s.GamesWon)
                .IsRequired()
                .HasDefaultValue(0)
                .HasColumnName("StatsGamesWon");

            stats.Property(s => s.GamesLost)
                .IsRequired()
                .HasDefaultValue(0)
                .HasColumnName("StatsGamesLost");

            stats.Property(s => s.TotalPlayTime)
                .IsRequired()
                .HasDefaultValue(TimeSpan.Zero)
                .HasColumnName("StatsTotalPlayTime");

            stats.Property(s => s.BestTimeOverall)
                .IsRequired()
                .HasDefaultValue(TimeSpan.MaxValue)
                .HasColumnName("StatsBestTimeOverall");

            // Store difficulty statistics as JSON
            stats.Property(s => s.DifficultyStats)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, DifficultyStatistics>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, DifficultyStatistics>())
                .HasColumnName("StatsDifficultyStats")
                .HasColumnType("text"); // Use text for SQLite compatibility
        });

        // Unique constraints
        builder.HasIndex(p => p.Username)
            .IsUnique()
            .HasDatabaseName("IX_Players_Username");

        builder.HasIndex(p => p.Email)
            .IsUnique()
            .HasDatabaseName("IX_Players_Email");

        // Indexes for performance
        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_Players_IsActive");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Players_CreatedAt");

        builder.HasIndex(p => p.LastLoginAt)
            .HasDatabaseName("IX_Players_LastLoginAt");

        // TODO: [ENHANCEMENT] Add indexes for leaderboard queries after migration
        // Note: Indexes on owned entity properties require special handling
        // Can be added via raw SQL in migration or using shadow properties
    }
}
