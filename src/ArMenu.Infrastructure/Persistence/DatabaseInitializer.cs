using ArMenu.Infrastructure.MultiTenancy;
using ArMenu.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArMenu.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations with the schema-owner connection, then seeds demo tenants through the regular
    /// runtime pipeline (tenant isolation interceptors and row-level security included).
    /// </summary>
    /// <remarks>
    /// Development convenience only. Production schema changes ship as reviewed migration bundles from CI/CD,
    /// never as a side effect of application startup.
    /// </remarks>
    public static async Task InitializeDevelopmentDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var migrationsConnectionString =
            configuration.GetConnectionString(ConnectionStringNames.Migrations)
            ?? configuration.GetConnectionString(ConnectionStringNames.Runtime)
            ?? throw new InvalidOperationException("No database connection string is configured.");

        var options = new DbContextOptionsBuilder<ArMenuDbContext>()
            .UseArMenuDatabase(migrationsConnectionString)
            .Options;

        await using (var dbContext = new ArMenuDbContext(options, new TenantContext()))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        await ActivatorUtilities
            .CreateInstance<DevelopmentDataSeeder>(services)
            .SeedAsync(cancellationToken);
    }
}
