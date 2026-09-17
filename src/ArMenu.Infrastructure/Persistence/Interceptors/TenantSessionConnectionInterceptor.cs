using System.Data.Common;
using ArMenu.Application.MultiTenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ArMenu.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Publishes the scope's tenant to PostgreSQL every time EF Core opens a (pooled) connection, as the session setting
/// <c>app.current_tenant</c>. Row-level security policies read that setting, so isolation holds even for queries
/// that bypass EF Core query filters: raw SQL, <c>IgnoreQueryFilters()</c>, or a filter someone forgot to add.
/// </summary>
/// <remarks>
/// An unbound scope publishes an empty value, which the policies treat as "no tenant": zero rows, never all rows.
/// </remarks>
internal sealed class TenantSessionConnectionInterceptor(ITenantContext tenantContext) : DbConnectionInterceptor
{
    public const string SettingName = "app.current_tenant";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using var command = CreateSetTenantCommand(connection);
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        await using var command = CreateSetTenantCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateSetTenantCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT set_config('{SettingName}', @tenant_id, false)";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "tenant_id";
        parameter.Value = tenantContext.Tenant?.Id.Value.ToString() ?? string.Empty;
        command.Parameters.Add(parameter);

        return command;
    }
}
