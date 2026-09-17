using ArMenu.Domain.Menus;
using ArMenu.Domain.Tenants;

namespace ArMenu.Application.Abstractions.Statistics;

/// <summary>Adds guest events to the daily counters of a tenant.</summary>
public interface IMenuStatisticsRecorder
{
    /// <summary>
    /// Adds <see cref="MenuEventCount.Count"/> to each counter. Events about a dish the tenant does not have are ignored,
    /// so made-up item ids create nothing.
    /// </summary>
    Task RecordAsync(TenantId tenantId, DateOnly day, IReadOnlyList<MenuEventCount> counts, CancellationToken cancellationToken);
}

/// <param name="Event">Stable event name, such as <c>dish_opened</c>.</param>
/// <param name="ItemId">The dish, or <see langword="null"/> for events about the whole menu.</param>
/// <param name="Count">How many times it happened in this batch.</param>
public sealed record MenuEventCount(string Event, MenuItemId? ItemId, int Count);
