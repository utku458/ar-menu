using ArMenu.Infrastructure.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ArMenu.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> only. Scaffolding migrations needs the model, not a live database, so no application host is built.
/// </summary>
internal sealed class DesignTimeArMenuDbContextFactory : IDesignTimeDbContextFactory<ArMenuDbContext>
{
    private const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=5432;Database=armenu;Username=postgres;Password=postgres";

    public ArMenuDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ARMENU_MIGRATIONS_CONNECTION_STRING") ?? LocalDevelopmentConnectionString;

        var options = new DbContextOptionsBuilder<ArMenuDbContext>().UseArMenuDatabase(connectionString).Options;

        return new ArMenuDbContext(options, new TenantContext());
    }
}
