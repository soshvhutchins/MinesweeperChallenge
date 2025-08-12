using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Minesweeper.Domain.Repositories;
using Minesweeper.Infrastructure.Data;
using Minesweeper.Infrastructure.Repositories;

namespace Minesweeper.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add Entity Framework with database provider selection
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var databaseProvider = configuration["Database:Provider"] ?? "SQLite";
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            switch (databaseProvider.ToUpperInvariant())
            {
                case "SQLITE":
                    connectionString ??= "Data Source=minesweeper.db";
                    options.UseSqlite(connectionString, sqliteOptions =>
                    {
                        sqliteOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    });
                    break;

                case "POSTGRESQL":
                    connectionString ??= "Host=localhost;Database=minesweeper;Username=postgres;Password=postgres";
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorCodesToAdd: null);
                    });
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported database provider: {databaseProvider}");
            }

            // Enable sensitive data logging in development
            var enableSensitiveLogging = configuration["Database:EnableSensitiveDataLogging"];
            if (bool.TryParse(enableSensitiveLogging, out var enableSensitive) && enableSensitive)
            {
                options.EnableSensitiveDataLogging();
            }

            // Enable detailed errors in development
            var enableDetailedErrors = configuration["Database:EnableDetailedErrors"];
            if (bool.TryParse(enableDetailedErrors, out var enableDetailed) && enableDetailed)
            {
                options.EnableDetailedErrors();
            }
        });

        // Register repositories
        services.AddScoped<IGameRepository, EfGameRepository>();
        services.AddScoped<IPlayerRepository, EfPlayerRepository>();

        return services;
    }
}
