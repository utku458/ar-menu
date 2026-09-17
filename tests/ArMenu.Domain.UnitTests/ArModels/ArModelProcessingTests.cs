using ArMenu.Domain.ArModels;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.UnitTests.TestSupport;

namespace ArMenu.Domain.UnitTests.ArModels;

public sealed class ArModelProcessingTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    private static readonly ArModelReport Report = new(
        source: new ModelStatistics(412_000, 208_400, 2, 3, 4096),
        optimized: new ModelStatistics(99_620, 51_250, 2, 3, 2048),
        files: new ModelFileSizes(18_350_211, 1_342_877, 3_921_064, 4_102_345, 38_112),
        dimensions: new ModelDimensions(0.182, 0.114, 0.179),
        warnings: ["model.simplified"]);

    private static ArModelProcessing NewProcessing() =>
        ArModelProcessing.Queue(TenantId.New(), MenuItemId.New(), sourceSize: 1024);

    private static ArModelProcessing Started()
    {
        var processing = NewProcessing();
        processing.Start(Now).ShouldSucceed();
        return processing;
    }

    [Fact]
    public void A_new_processing_waits_in_the_queue()
    {
        var processing = NewProcessing();

        processing.Status.ShouldBe(ArModelProcessingStatus.Queued);
        processing.Attempts.ShouldBe(0);
        processing.IsFinished.ShouldBeFalse();
    }

    [Fact]
    public void Succeeding_keeps_the_report_and_finishes()
    {
        var processing = Started();

        processing.Succeed(Report, Now).ShouldSucceed();

        processing.Status.ShouldBe(ArModelProcessingStatus.Succeeded);
        processing.Report.ShouldBe(Report);
        processing.CompletedAt.ShouldBe(Now);
        processing.FailureCode.ShouldBeNull();
    }

    [Fact]
    public void Only_a_started_processing_can_succeed()
    {
        NewProcessing().Succeed(Report, Now).ShouldFailWith(ArModelProcessingErrors.NotStarted);
    }

    [Fact]
    public void A_rejected_upload_fails_with_its_reason()
    {
        var processing = Started();

        processing.Reject("model.texture_unsupported", Now).ShouldSucceed();

        processing.Status.ShouldBe(ArModelProcessingStatus.Failed);
        processing.FailureCode.ShouldBe("model.texture_unsupported");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void A_rejection_needs_a_code(string failureCode)
    {
        Started().Reject(failureCode, Now).ShouldFailWith(ArModelProcessingErrors.FailureCodeInvalid);
    }

    [Fact]
    public void Transient_failures_are_retried_until_the_attempts_are_used_up()
    {
        var processing = NewProcessing();

        for (var attempt = 1; attempt < ArModelProcessing.MaxAttempts; attempt++)
        {
            processing.Start(Now).ShouldSucceed();
            processing.RetryOrGiveUp(Now).ShouldSucceed().ShouldBeTrue();
            processing.Status.ShouldBe(ArModelProcessingStatus.Queued);
        }

        processing.Start(Now).ShouldSucceed();
        processing.RetryOrGiveUp(Now).ShouldSucceed().ShouldBeFalse();

        processing.Status.ShouldBe(ArModelProcessingStatus.Failed);
        processing.FailureCode.ShouldBe(ArModelProcessingErrors.Unavailable.Code);
        processing.Attempts.ShouldBe(ArModelProcessing.MaxAttempts);
    }

    [Fact]
    public void Attempts_abandoned_by_crashed_workers_count_too()
    {
        var processing = NewProcessing();

        // A worker claims the processing, then dies without recording anything; the lease expires and another claims it.
        for (var attempt = 0; attempt < ArModelProcessing.MaxAttempts; attempt++)
        {
            processing.Start(Now).ShouldSucceed();
        }

        processing.Start(Now).ShouldFailWith(ArModelProcessingErrors.Unavailable);
        processing.Status.ShouldBe(ArModelProcessingStatus.Failed);
    }

    [Fact]
    public void A_newer_upload_supersedes_an_unfinished_processing()
    {
        var processing = Started();

        processing.Supersede(Now).ShouldSucceed();

        processing.Status.ShouldBe(ArModelProcessingStatus.Superseded);
        processing.IsFinished.ShouldBeTrue();
    }

    [Fact]
    public void A_finished_processing_cannot_change_anymore()
    {
        var processing = Started();
        processing.Supersede(Now).ShouldSucceed();

        processing.Start(Now).ShouldFailWith(ArModelProcessingErrors.AlreadyFinished);
        processing.Succeed(Report, Now).ShouldFailWith(ArModelProcessingErrors.AlreadyFinished);
        processing.Reject("model.empty", Now).ShouldFailWith(ArModelProcessingErrors.AlreadyFinished);
        processing.Supersede(Now).ShouldFailWith(ArModelProcessingErrors.AlreadyFinished);
        processing.Status.ShouldBe(ArModelProcessingStatus.Superseded);
    }
}
