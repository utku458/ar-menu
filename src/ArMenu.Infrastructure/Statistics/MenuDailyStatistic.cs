using ArMenu.Domain.Common;
using ArMenu.Domain.Tenants;

namespace ArMenu.Infrastructure.Statistics;

/// <summary>
/// One counter: how often an event happened on a tenant's menu (or one of its dishes) on a day. A persistence model,
/// not a domain aggregate: counters are written with an atomic upsert and read as projections.
/// </summary>
internal sealed class MenuDailyStatistic : ITenantScoped
{
    /// <summary>Stands for "the whole menu" in <see cref="MenuItemId"/>, so the counter's key needs no nullable column.</summary>
    public static readonly Guid WholeMenu = Guid.Empty;

    public TenantId TenantId { get; init; }

    public DateOnly Day { get; init; }

    public string Event { get; init; } = "";

    public Guid MenuItemId { get; init; }

    public long Count { get; init; }
}
