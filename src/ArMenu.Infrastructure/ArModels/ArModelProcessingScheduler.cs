using ArMenu.Application.ArModels;
using ArMenu.Domain.ArModels;
using ArMenu.Infrastructure.Persistence;

namespace ArMenu.Infrastructure.ArModels;

internal sealed class ArModelProcessingScheduler(ArMenuDbContext dbContext, ArModelProcessingSignal signal) : IArModelProcessingScheduler
{
    public void Schedule(ArModelProcessing processing, DateTimeOffset availableAt) =>
        dbContext.Set<ArModelProcessingQueueEntry>().Add(ArModelProcessingQueueEntry.For(processing, availableAt));

    public async Task RescheduleAsync(ArModelProcessingId processingId, DateTimeOffset availableAt, CancellationToken cancellationToken)
    {
        var entry = await dbContext.Set<ArModelProcessingQueueEntry>().FindAsync([processingId], cancellationToken);
        entry?.Reschedule(availableAt);
    }

    public async Task CompleteAsync(ArModelProcessingId processingId, CancellationToken cancellationToken)
    {
        var entry = await dbContext.Set<ArModelProcessingQueueEntry>().FindAsync([processingId], cancellationToken);
        if (entry is not null)
        {
            dbContext.Set<ArModelProcessingQueueEntry>().Remove(entry);
        }
    }

    public void Wake() => signal.Notify();
}
