using ArMenu.Infrastructure.MultiTenancy;
using ArMenu.Infrastructure.Persistence;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(PostgresDatabaseFixture))]

namespace ArMenu.IntegrationTests.TestSupport;

/// <summary>
/// One disposable PostgreSQL 18 server for the whole test assembly, provisioned exactly like local development:
/// the same role bootstrap script, then migrations applied by the schema owner. Tests talk to it through the
/// unprivileged runtime role, so row-level security is genuinely in force.
/// </summary>
public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private const string RuntimeRole = "armenu_app";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    /// <summary>Superuser connection: bypasses row-level security. Use it only to arrange or inspect data.</summary>
    public string OwnerConnectionString => _container.GetConnectionString();

    /// <summary>Connection of the runtime role, the one the API uses.</summary>
    public string RuntimeConnectionString => new NpgsqlConnectionStringBuilder(OwnerConnectionString)
    {
        Username = RuntimeRole,
        Password = RuntimeRole,
    }.ConnectionString;

    /// <summary>Infrastructure services wired exactly as in production, connected as the runtime role.</summary>
    public TestServices Services { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var bootstrapScript = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Database", "01-create-app-role.sql"));
        await ExecuteAsOwnerAsync(bootstrapScript);

        var ownerOptions = new DbContextOptionsBuilder<ArMenuDbContext>().UseArMenuDatabase(OwnerConnectionString).Options;
        await using (var ownerContext = new ArMenuDbContext(ownerOptions, new TenantContext()))
        {
            await ownerContext.Database.MigrateAsync();
        }

        Services = new TestServices(RuntimeConnectionString);
    }

    public async Task<int> ExecuteAsOwnerAsync(string sql, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(OwnerConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<T> ScalarAsOwnerAsync<T>(string sql, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(OwnerConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async ValueTask DisposeAsync()
    {
        if (Services is not null)
        {
            await Services.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}
