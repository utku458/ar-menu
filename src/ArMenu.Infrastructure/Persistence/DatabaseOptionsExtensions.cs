using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Persistence;

internal static class DatabaseOptionsExtensions
{
    /// <summary>
    /// Provider configuration shared by the runtime, the migration runner and the design-time factory,
    /// so the model EF Core builds is identical everywhere.
    /// </summary>
    public static TBuilder UseArMenuDatabase<TBuilder>(this TBuilder builder, string connectionString)
        where TBuilder : DbContextOptionsBuilder
    {
        builder
            .UseNpgsql(connectionString, npgsql => npgsql
                .MigrationsHistoryTable("__ef_migrations_history")
                .EnableRetryOnFailure(maxRetryCount: 3))
            .UseSnakeCaseNamingConvention();

        return builder;
    }
}
