using System.Diagnostics;
using ArMenu.Application.Common.Behaviors;
using ArMenu.Application.Common.Diagnostics;
using ArMenu.Application.UnitTests.TestSupport;
using ArMenu.Domain.Common;
using Mediator;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace ArMenu.Application.UnitTests.Common;

public sealed class TelemetryBehaviorTests : IDisposable
{
    private readonly TestMeterFactory _meterFactory = new();
    private readonly MetricCollector<double> _durations;
    private readonly TelemetryBehavior<PublishDishCommand, Result> _behavior;

    public TelemetryBehaviorTests()
    {
        _durations = new MetricCollector<double>(_meterFactory, ArMenuTelemetry.Name, "armenu.use_case.duration");
        _behavior = new TelemetryBehavior<PublishDishCommand, Result>(new ArMenuMetrics(_meterFactory));
    }

    [Fact]
    public async Task Successful_use_cases_are_measured_by_name()
    {
        await _behavior.Handle(new PublishDishCommand(), (_, _) => ValueTask.FromResult(Result.Success()), TestContext.Current.CancellationToken);

        var measurement = _durations.GetMeasurementSnapshot().ShouldHaveSingleItem();
        measurement.Tags[ArMenuTelemetry.Tags.UseCase].ShouldBe(nameof(PublishDishCommand));
        measurement.Tags[ArMenuTelemetry.Tags.Outcome].ShouldBe("success");
        measurement.Tags.ShouldNotContainKey(ArMenuTelemetry.Tags.ErrorType);
    }

    [Fact]
    public async Task Failures_carry_their_error_code_on_the_metric_and_the_span()
    {
        Activity? span = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ArMenuTelemetry.Name,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => span = activity,
        };
        ActivitySource.AddActivityListener(listener);

        await _behavior.Handle(
            new PublishDishCommand(),
            (_, _) => ValueTask.FromResult(Result.Failure(Error.Conflict("dish.already_published", "Already published."))),
            TestContext.Current.CancellationToken);

        var measurement = _durations.GetMeasurementSnapshot().ShouldHaveSingleItem();
        measurement.Tags[ArMenuTelemetry.Tags.Outcome].ShouldBe("failure");
        measurement.Tags[ArMenuTelemetry.Tags.ErrorType].ShouldBe("dish.already_published");

        span.ShouldNotBeNull();
        span.DisplayName.ShouldBe(nameof(PublishDishCommand));
        span.GetTagItem(ArMenuTelemetry.Tags.ErrorType).ShouldBe("dish.already_published");
        // An expected failure is an answer, not a fault.
        span.Status.ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task Exceptions_mark_the_span_as_an_error_and_still_count()
    {
        await Should.ThrowAsync<TimeoutException>(async () => await _behavior.Handle(
            new PublishDishCommand(),
            (_, _) => throw new TimeoutException(),
            TestContext.Current.CancellationToken));

        _durations.GetMeasurementSnapshot().ShouldHaveSingleItem().Tags[ArMenuTelemetry.Tags.ErrorType].ShouldBe(typeof(TimeoutException).FullName);
    }

    public void Dispose()
    {
        _durations.Dispose();
        _meterFactory.Dispose();
    }

    internal sealed record PublishDishCommand : ICommand<Result>;
}
