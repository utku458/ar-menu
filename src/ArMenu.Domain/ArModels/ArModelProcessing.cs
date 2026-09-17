using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Tenants;

namespace ArMenu.Domain.ArModels;

/// <summary>
/// One upload of a 3D model on its way to a menu item: queued, processed into every file guests' devices need, and
/// attached to the item, or failed with a reason the business can act on.
/// </summary>
/// <remarks>
/// Processing is slow and runs outside the request, so its state is an aggregate of its own rather than part of
/// <see cref="MenuItem"/>: the item keeps serving its current model until a new one is ready, and editing the item
/// never contends with a worker. Files are addressed by the processing's id, so a retry overwrites its own outputs and
/// can never touch another upload's.
/// </remarks>
public sealed class ArModelProcessing : AggregateRoot<ArModelProcessingId>, ITenantScoped, IAuditable
{
    /// <summary>Attempts before giving up, counting attempts a crashed worker never finished.</summary>
    public const int MaxAttempts = 3;

    public const int FailureCodeMaxLength = 100;

    private ArModelProcessing(ArModelProcessingId id, TenantId tenantId, MenuItemId menuItemId, long sourceSize)
        : base(id)
    {
        TenantId = tenantId;
        MenuItemId = menuItemId;
        SourceSize = sourceSize;
        Status = ArModelProcessingStatus.Queued;
    }

#pragma warning disable CS8618 // Used by EF Core; every property is populated during materialization.
    private ArModelProcessing()
    {
    }
#pragma warning restore CS8618

    public TenantId TenantId { get; private init; }

    public MenuItemId MenuItemId { get; private init; }

    /// <summary>Size in bytes of the uploaded file.</summary>
    public long SourceSize { get; private init; }

    public ArModelProcessingStatus Status { get; private set; }

    public int Attempts { get; private set; }

    /// <summary>Stable code of why processing failed, such as <c>model.texture_unsupported</c>.</summary>
    public string? FailureCode { get; private set; }

    public ArModelReport? Report { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public bool IsFinished => Status is ArModelProcessingStatus.Succeeded or ArModelProcessingStatus.Failed or ArModelProcessingStatus.Superseded;

    public static ArModelProcessing Queue(TenantId tenantId, MenuItemId menuItemId, long sourceSize)
    {
        Guard.NotDefault(tenantId);
        Guard.NotDefault(menuItemId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceSize);

        return new ArModelProcessing(ArModelProcessingId.New(), tenantId, menuItemId, sourceSize);
    }

    /// <summary>
    /// Begins an attempt. A processing found still running was abandoned by a crashed worker; that attempt counts, and
    /// once every attempt is used up the processing fails instead of starting again.
    /// </summary>
    public Result Start(DateTimeOffset now)
    {
        if (IsFinished)
        {
            return ArModelProcessingErrors.AlreadyFinished;
        }

        if (Attempts >= MaxAttempts)
        {
            Finish(ArModelProcessingStatus.Failed, ArModelProcessingErrors.Unavailable.Code, now);
            return ArModelProcessingErrors.Unavailable;
        }

        Status = ArModelProcessingStatus.Processing;
        Attempts++;
        return Result.Success();
    }

    public Result Succeed(ArModelReport report, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (Status != ArModelProcessingStatus.Processing)
        {
            return IsFinished ? ArModelProcessingErrors.AlreadyFinished : ArModelProcessingErrors.NotStarted;
        }

        Report = report;
        Finish(ArModelProcessingStatus.Succeeded, failureCode: null, now);
        return Result.Success();
    }

    /// <summary>Fails for good: the upload itself is the problem, so trying again cannot help.</summary>
    public Result Reject(string failureCode, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(failureCode) || failureCode.Length > FailureCodeMaxLength)
        {
            return ArModelProcessingErrors.FailureCodeInvalid;
        }

        if (IsFinished)
        {
            return ArModelProcessingErrors.AlreadyFinished;
        }

        Finish(ArModelProcessingStatus.Failed, failureCode, now);
        return Result.Success();
    }

    /// <summary>
    /// Records an attempt that failed for reasons unrelated to the upload. The processing is queued again while attempts
    /// remain, and fails as <see cref="ArModelProcessingErrors.Unavailable"/> once they are used up.
    /// </summary>
    /// <returns>Whether the processing will be retried.</returns>
    public Result<bool> RetryOrGiveUp(DateTimeOffset now)
    {
        if (Status != ArModelProcessingStatus.Processing)
        {
            return IsFinished ? ArModelProcessingErrors.AlreadyFinished : ArModelProcessingErrors.NotStarted;
        }

        if (Attempts < MaxAttempts)
        {
            Status = ArModelProcessingStatus.Queued;
            return true;
        }

        Finish(ArModelProcessingStatus.Failed, ArModelProcessingErrors.Unavailable.Code, now);
        return false;
    }

    /// <summary>A newer upload for the same item wins; whatever this processing still produces is discarded.</summary>
    public Result Supersede(DateTimeOffset now)
    {
        if (IsFinished)
        {
            return ArModelProcessingErrors.AlreadyFinished;
        }

        Finish(ArModelProcessingStatus.Superseded, failureCode: null, now);
        return Result.Success();
    }

    private void Finish(ArModelProcessingStatus status, string? failureCode, DateTimeOffset now)
    {
        Status = status;
        FailureCode = failureCode;
        CompletedAt = now;
    }
}
