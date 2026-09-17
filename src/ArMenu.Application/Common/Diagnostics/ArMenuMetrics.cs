using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ArMenu.Application.Common.Diagnostics;

/// <summary>
/// The business-level signals operators watch: how use cases fail, how model processing goes, what cleanup deletes and
/// whether e-mail gets out. HTTP, database and runtime metrics come from instrumentation libraries.
/// </summary>
public sealed class ArMenuMetrics
{
    private readonly Histogram<double> _useCaseDuration;
    private readonly Counter<long> _processingAttempts;
    private readonly Histogram<double> _processingDuration;
    private readonly Histogram<double> _timeToPublish;
    private readonly Counter<long> _cleanedUpFiles;
    private readonly Counter<long> _emails;
    private readonly Counter<long> _menuEvents;
    private readonly Counter<long> _purgedTenants;

    public ArMenuMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        var meter = meterFactory.Create(ArMenuTelemetry.Name);

        _useCaseDuration = meter.CreateHistogram(
            "armenu.use_case.duration",
            unit: "s",
            description: "Duration of commands and queries, by outcome and error code.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10] });

        _processingAttempts = meter.CreateCounter<long>(
            "armenu.ar_model.processing.attempts",
            unit: "{attempt}",
            description: "Model processing attempts, by outcome: succeeded, rejected, retrying or failed.");

        _processingDuration = meter.CreateHistogram(
            "armenu.ar_model.processing.duration",
            unit: "s",
            description: "Time the asset processor took for one attempt.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [1, 2.5, 5, 10, 20, 30, 60, 120, 300] });

        _timeToPublish = meter.CreateHistogram(
            "armenu.ar_model.processing.time_to_publish",
            unit: "s",
            description: "Time from upload to the model going live, retries and queueing included.",
            advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [5, 10, 20, 30, 60, 120, 300, 600, 1800] });

        _cleanedUpFiles = meter.CreateCounter<long>(
            "armenu.assets.cleanup.deleted",
            unit: "{file}",
            description: "Unused files deleted by asset cleanup, by storage location.");

        _emails = meter.CreateCounter<long>(
            "armenu.email.messages",
            unit: "{message}",
            description: "Transactional e-mails, by template and outcome: sent or failed.");

        _purgedTenants = meter.CreateCounter<long>(
            "armenu.tenants.purged",
            unit: "{tenant}",
            description: "Closed businesses whose data and files were deleted after the retention period.");

        _menuEvents = meter.CreateCounter<long>(
            "armenu.menu.events",
            unit: "{event}",
            description: "Guest events on menus, by type. Not tagged by tenant, which would make one series per business.");
    }

    public void UseCaseCompleted(string useCase, TimeSpan duration, string? errorCode)
    {
        var tags = new TagList
        {
            { ArMenuTelemetry.Tags.UseCase, useCase },
            { ArMenuTelemetry.Tags.Outcome, errorCode is null ? "success" : "failure" },
        };

        if (errorCode is not null)
        {
            tags.Add(ArMenuTelemetry.Tags.ErrorType, errorCode);
        }

        _useCaseDuration.Record(duration.TotalSeconds, tags);
    }

    public void ProcessingAttempted(ProcessingOutcome outcome, TimeSpan duration, string? errorCode = null)
    {
        var tags = new TagList { { ArMenuTelemetry.Tags.Outcome, OutcomeName(outcome) } };
        if (errorCode is not null)
        {
            tags.Add(ArMenuTelemetry.Tags.ErrorType, errorCode);
        }

        _processingAttempts.Add(1, tags);
        _processingDuration.Record(duration.TotalSeconds, tags);
    }

    public void ModelPublished(TimeSpan sinceUpload) => _timeToPublish.Record(sinceUpload.TotalSeconds);

    public void FilesCleanedUp(string location, int count)
    {
        if (count > 0)
        {
            _cleanedUpFiles.Add(count, new KeyValuePair<string, object?>(ArMenuTelemetry.Tags.AssetLocation, location));
        }
    }

    public void EmailAttempted(string template, bool sent) =>
        _emails.Add(
            1,
            new KeyValuePair<string, object?>(ArMenuTelemetry.Tags.EmailTemplate, template),
            new KeyValuePair<string, object?>(ArMenuTelemetry.Tags.Outcome, sent ? "sent" : "failed"));

    public void TenantPurged() => _purgedTenants.Add(1);

    public void MenuEventsRecorded(string eventName, int count) =>
        _menuEvents.Add(count, new KeyValuePair<string, object?>(ArMenuTelemetry.Tags.MenuEvent, eventName));

    private static string OutcomeName(ProcessingOutcome outcome) => outcome switch
    {
        ProcessingOutcome.Succeeded => "succeeded",
        ProcessingOutcome.Rejected => "rejected",
        ProcessingOutcome.Retrying => "retrying",
        ProcessingOutcome.Failed => "failed",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
    };

    public enum ProcessingOutcome
    {
        Succeeded,
        Rejected,
        Retrying,
        Failed,
    }
}
