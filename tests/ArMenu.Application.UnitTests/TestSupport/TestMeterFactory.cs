using System.Diagnostics.Metrics;

namespace ArMenu.Application.UnitTests.TestSupport;

/// <summary>Meters scoped to one test, so a <c>MetricCollector</c> sees nothing recorded by other tests.</summary>
internal sealed class TestMeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = [];

    public Meter Create(MeterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var meter = new Meter(options.Name, options.Version, options.Tags, scope: this);
        _meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        foreach (var meter in _meters)
        {
            meter.Dispose();
        }
    }
}
