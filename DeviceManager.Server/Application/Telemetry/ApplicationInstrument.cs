namespace DeviceManager.Server.Application.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

public sealed class ApplicationInstrument : IDisposable
{
    private readonly TimeProvider timeProvider;

    private readonly long startTimestamp;

    private readonly Meter meter;

    public ActivitySource ActivitySource { get; }

    public ApplicationInstrument(TimeProvider timeProvider, IMeterFactory meterFactory)
    {
        this.timeProvider = timeProvider;
        startTimestamp = timeProvider.GetTimestamp();

        ActivitySource = new ActivitySource(Source.Name, Source.Version);
        meter = meterFactory.Create(Source.Name, Source.Version);

        meter.CreateObservableCounter("application.uptime", ObserveApplicationUptime);
    }

    public void Dispose()
    {
        ActivitySource.Dispose();
        meter.Dispose();
    }

    private long ObserveApplicationUptime() =>
        (long)timeProvider.GetElapsedTime(startTimestamp).TotalSeconds;
}
