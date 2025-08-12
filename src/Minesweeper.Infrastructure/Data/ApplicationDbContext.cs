using Microsoft.EntityFrameworkCore;
using Minesweeper.Domain.Aggregates;
using Minesweeper.Domain.Common;
using Minesweeper.Infrastructure.Data.Configurations;
using System.Collections;

namespace Minesweeper.Infrastructure.Data;

/// <summary>
/// Application database context for Minesweeper game
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Game> Games => Set<Game>();
    public DbSet<Player> Players => Set<Player>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new GameConfiguration());
        modelBuilder.ApplyConfiguration(new PlayerConfiguration());

        // Configure value object conversions
        ConfigureValueObjects(modelBuilder);

        // Configure domain events (exclude from database)
        ConfigureDomainEvents(modelBuilder);
    }

    private static void ConfigureValueObjects(ModelBuilder modelBuilder)
    {
        // GameId value object conversions
        modelBuilder.Entity<Game>()
            .Property(e => e.Id)
            .HasConversion(
                v => v.Value,
                v => new Domain.ValueObjects.GameId(v));

        // PlayerId value object conversions for Game
        modelBuilder.Entity<Game>()
            .Property(e => e.PlayerId)
            .HasConversion(
                v => v.Value,
                v => new Domain.ValueObjects.PlayerId(v));

        // PlayerId value object conversions for Player
        modelBuilder.Entity<Player>()
            .Property(e => e.Id)
            .HasConversion(
                v => v.Value,
                v => new Domain.ValueObjects.PlayerId(v));
    }

    private static void ConfigureDomainEvents(ModelBuilder modelBuilder)
    {
        // Ignore domain events from all entities that inherit from Entity base class
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var entityClrType = entityType.ClrType;

            // Check if this is an Entity type by checking if it has a DomainEvents property
            var domainEventsProperty = entityClrType.GetProperty("DomainEvents");
            if (domainEventsProperty != null)
            {
                modelBuilder.Entity(entityClrType).Ignore("DomainEvents");
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps before saving
        UpdateTimestamps();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Clear domain events after successful save
        ClearDomainEvents();

        return result;
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // Add any timestamp updates here if needed
            // For now, entities manage their own timestamps
        }
    }

    private void ClearDomainEvents()
    {
        var entitiesWithEvents = ChangeTracker.Entries()
            .Where(e => e.Entity.GetType().GetProperty("DomainEvents") != null)
            .Select(e => e.Entity)
            .ToArray();

        foreach (var entity in entitiesWithEvents)
        {
            var domainEventsProperty = entity.GetType().GetProperty("DomainEvents");
            var domainEvents = domainEventsProperty?.GetValue(entity) as IEnumerable;
            if (domainEvents != null && domainEvents.Cast<object>().Any())
            {
                // Call ClearDomainEvents method if it exists
                var clearMethod = entity.GetType().GetMethod("ClearDomainEvents");
                clearMethod?.Invoke(entity, null);
            }
        }
    }
}
