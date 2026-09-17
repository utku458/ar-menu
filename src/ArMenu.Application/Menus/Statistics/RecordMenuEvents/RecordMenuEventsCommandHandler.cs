using ArMenu.Application.Abstractions.Statistics;
using ArMenu.Application.Common.Diagnostics;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Statistics.RecordMenuEvents;

public sealed class RecordMenuEventsCommandHandler(
    ITenantContext tenantContext,
    IMenuStatisticsRecorder recorder,
    ArMenuMetrics metrics,
    TimeProvider timeProvider)
    : ICommandHandler<RecordMenuEventsCommand, Result>
{
    public async ValueTask<Result> Handle(RecordMenuEventsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var counts = command.Events
            .GroupBy(input => (input.Type, input.ItemId))
            .Select(group => new MenuEventCount(
                group.Key.Type.Name(),
                group.Key.ItemId is { } itemId ? MenuItemId.From(itemId) : null,
                group.Count()))
            .ToList();

        // The business's own day: a guest at 01:00 in Istanbul belongs to the evening that is still going on.
        var day = tenantContext.RequireTenant().DateAt(timeProvider.GetUtcNow());
        await recorder.RecordAsync(tenantContext.TenantId, day, counts, cancellationToken);

        foreach (var count in counts)
        {
            metrics.MenuEventsRecorded(count.Event, count.Count);
        }

        return Result.Success();
    }
}
