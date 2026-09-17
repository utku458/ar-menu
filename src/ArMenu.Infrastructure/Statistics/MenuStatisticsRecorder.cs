using ArMenu.Application.Abstractions.Statistics;
using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.Persistence;
using ArMenu.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Statistics;

/// <summary>
/// Increments counters with <c>INSERT … ON CONFLICT DO UPDATE</c>: concurrent guests add up instead of overwriting each
/// other, with no read first. Row-level security applies as to any tenant table.
/// </summary>
internal sealed class MenuStatisticsRecorder(ArMenuDbContext dbContext) : IMenuStatisticsRecorder
{
    private static readonly string UpsertSql = $$"""
        INSERT INTO {{MenuDailyStatisticConfiguration.TableName}} (tenant_id, day, event, menu_item_id, count)
        SELECT {0}, {1}, {2}, {3}, {4}
        WHERE {3} = '00000000-0000-0000-0000-000000000000'::uuid
           OR EXISTS (SELECT 1 FROM menu_items WHERE tenant_id = {0} AND id = {3} AND NOT is_deleted)
        ON CONFLICT (tenant_id, day, event, menu_item_id)
        DO UPDATE SET count = {{MenuDailyStatisticConfiguration.TableName}}.count + EXCLUDED.count
        """;

    public async Task RecordAsync(TenantId tenantId, DateOnly day, IReadOnlyList<MenuEventCount> counts, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(counts);

        foreach (var count in counts)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                UpsertSql,
                [tenantId.Value, day, count.Event, count.ItemId?.Value ?? MenuDailyStatistic.WholeMenu, (long)count.Count],
                cancellationToken);
        }
    }
}
