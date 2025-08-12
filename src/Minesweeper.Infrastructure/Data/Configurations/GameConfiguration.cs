using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Minesweeper.Domain.Aggregates;
using Minesweeper.Domain.Enums;
using System.Text.Json;

namespace Minesweeper.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Game aggregate
/// </summary>
public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("Games");

        // Primary key
        builder.HasKey(g => g.Id);

        // Properties
        builder.Property(g => g.PlayerIdValue)
            .IsRequired()
            .HasColumnName("PlayerId")
            .HasMaxLength(36);

        builder.Property(g => g.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(g => g.StartedAt)
            .IsRequired()
            .HasColumnName("StartedAt");

        builder.Property(g => g.CompletedAt)
            .HasColumnName("CompletedAt");

        builder.Property(g => g.IsFirstMove)
            .IsRequired()
            .HasDefaultValue(true);

        // Store difficulty information as separate columns
        builder.Property(g => g.DifficultyName)
            .HasMaxLength(50)
            .IsRequired()
            .HasColumnName("DifficultyName");

        builder.Property(g => g.BoardRows)
            .IsRequired()
            .HasColumnName("BoardRows");

        builder.Property(g => g.BoardColumns)
            .IsRequired()
            .HasColumnName("BoardColumns");

        builder.Property(g => g.MineCount)
            .IsRequired()
            .HasColumnName("MineCount");

        // Ignore complex properties that are reconstructed in repositories
        builder.Ignore(g => g.PlayerId);
        builder.Ignore(g => g.Board);

        // Indexes for performance
        builder.HasIndex(g => g.PlayerIdValue)
            .HasDatabaseName("IX_Games_PlayerId");

        builder.HasIndex(g => g.Status)
            .HasDatabaseName("IX_Games_Status");

        builder.HasIndex(g => g.StartedAt)
            .HasDatabaseName("IX_Games_StartedAt");
    }
}
