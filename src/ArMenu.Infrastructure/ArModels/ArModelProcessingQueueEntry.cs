using ArMenu.Domain.ArModels;
using ArMenu.Domain.Tenants;

namespace ArMenu.Infrastructure.ArModels;

/// <summary>
/// A pointer to a processing waiting for a worker. Workers must find work across all tenants, which row-level security
/// rightly forbids on tenant data, so the queue holds nothing but ids and schedule: running a job binds the scope to its
/// tenant, and everything about the job is then read through row-level security.
/// </summary>
internal sealed class ArModelProcessingQueueEntry
{
    private ArModelProcessingQueueEntry(ArModelProcessingId processingId, TenantId tenantId, DateTimeOffset availableAt)
    {
        ProcessingId = processingId;
        TenantId = tenantId;
        AvailableAt = availableAt;
    }

    public ArModelProcessingId ProcessingId { get; private init; }

    public TenantId TenantId { get; private init; }

    /// <summary>Earliest time a worker may claim the entry.</summary>
    public DateTimeOffset AvailableAt { get; private set; }

    /// <summary>Until then, the worker that claimed the entry owns it. An expired lease means the worker died.</summary>
    public DateTimeOffset? LeaseExpiresAt { get; private set; }

    public static ArModelProcessingQueueEntry For(ArModelProcessing processing, DateTimeOffset availableAt)
    {
        ArgumentNullException.ThrowIfNull(processing);
        return new ArModelProcessingQueueEntry(processing.Id, processing.TenantId, availableAt);
    }

    public void Reschedule(DateTimeOffset availableAt)
    {
        AvailableAt = availableAt;
        LeaseExpiresAt = null;
    }
}
