using System.Diagnostics.Metrics;
using ArMenu.Application.Abstractions.Email;
using ArMenu.Application.Common.Diagnostics;
using ArMenu.Infrastructure.Maintenance;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.Extensions.Time.Testing;

namespace ArMenu.IntegrationTests.Persistence;

/// <summary>
/// The database is shared with tests running at the same time, so these compare with what was there before and never
/// expect exact totals. The clock is set far in the future: no other test's worker would claim these messages.
/// </summary>
public sealed class OperationalBacklogTests(PostgresDatabaseFixture database) : IAsyncDisposable
{
    private static readonly DateTimeOffset Start = new(2099, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(Start);
    private TestServices? _services;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Waiting_emails_show_how_many_there_are_and_how_long_the_oldest_has_waited()
    {
        var before = await Backlog.SampleAsync(Ct);

        await using (var scope = Services.BeginScope())
        {
            scope.Get<IEmailOutbox>().Add(new EmailMessage($"backlog-{Guid.NewGuid():N}@armenu.test", "S", "T", "<p>T</p>", "tr"), "test");
            scope.Get<IEmailOutbox>().Add(new EmailMessage($"backlog-{Guid.NewGuid():N}@armenu.test", "S", "T", "<p>T</p>", "tr"), "test");
            await scope.Db.SaveChangesAsync(Ct);
        }

        _time.Advance(TimeSpan.FromHours(2));
        var after = await Backlog.SampleAsync(Ct);

        after.EmailsPending.ShouldBeGreaterThanOrEqualTo(before.EmailsPending + 2);
        after.OldestEmailAge.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromHours(2));
    }

    [Fact]
    public async Task A_closed_business_kept_a_day_past_its_retention_counts_as_an_overdue_purge()
    {
        var seeded = await Services.SeedTenantAsync(Ct);
        await database.ExecuteAsOwnerAsync(
            $"UPDATE tenants SET status = 'Closed', closed_at = '{Start:O}' WHERE id = '{seeded.Tenant.Id.Value}'", Ct);

        _time.Advance(TimeSpan.FromDays(31) + TimeSpan.FromMinutes(1));

        (await Backlog.SampleAsync(Ct)).TenantsOverdueForPurge.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Gauges_report_nothing_until_a_sample_was_taken_then_its_values()
    {
        var gauges = Services.Get<BacklogGauges>();
        var observed = new Dictionary<string, double>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == ArMenuTelemetry.Name && instrument.Name.StartsWith("armenu.email.outbox", StringComparison.Ordinal))
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) => observed[instrument.Name] = value);
        listener.SetMeasurementEventCallback<double>((instrument, value, _, _) => observed[instrument.Name] = value);
        listener.Start();

        listener.RecordObservableInstruments();
        observed.ShouldBeEmpty();

        gauges.Publish(new BacklogSnapshot(7, TimeSpan.FromMinutes(3), 0, TimeSpan.Zero, 0));
        listener.RecordObservableInstruments();

        observed["armenu.email.outbox.pending"].ShouldBe(7);
        observed["armenu.email.outbox.oldest_age"].ShouldBe(180);
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }
    }

    private TestServices Services => _services ??= new TestServices(database.RuntimeConnectionString, _time);

    private OperationalBacklog Backlog => Services.Get<OperationalBacklog>();
}
