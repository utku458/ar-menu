using ArMenu.Domain.ArModels;

namespace ArMenu.Application.ArModels;

/// <summary>
/// Schedules processings for the workers, backed by a queue. Changes join the current unit of work, so a processing and
/// its place in the queue are always saved together.
/// </summary>
public interface IArModelProcessingScheduler
{
    void Schedule(ArModelProcessing processing, DateTimeOffset availableAt);

    /// <summary>Makes the processing available again at <paramref name="availableAt"/> and releases its lease.</summary>
    Task RescheduleAsync(ArModelProcessingId processingId, DateTimeOffset availableAt, CancellationToken cancellationToken);

    /// <summary>Removes the processing from the queue: it finished, or there is nothing left to run.</summary>
    Task CompleteAsync(ArModelProcessingId processingId, CancellationToken cancellationToken);

    /// <summary>Tells workers in this process that work is waiting, instead of letting them find it on their next poll.</summary>
    void Wake();
}
