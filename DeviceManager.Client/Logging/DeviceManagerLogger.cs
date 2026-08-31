namespace DeviceManager.Client.Logging;

using DeviceManager.Client.Telemetry;

// Microsoft.Extensions.Logging から端末ログをテレメトリへ転送するロガー
internal sealed class DeviceManagerLogger : ILogger
{
    private readonly string category;

    private readonly TelemetryOptions options;

    private readonly DeviceManagerTelemetry telemetry;

    public DeviceManagerLogger(string category, TelemetryOptions options, DeviceManagerTelemetry telemetry)
    {
        this.category = category;
        this.options = options;
        this.telemetry = telemetry;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) =>
        (logLevel != LogLevel.None) &&
        ((int)logLevel >= (int)options.LogMinLevel) &&
        // SDK 自身のログはフィードバックループ防止のため転送しない
        !category.StartsWith("DeviceManager.Client", StringComparison.Ordinal);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        telemetry.SendLog(new DeviceLogRecord
        {
            Level = (DeviceLogLevel)(int)logLevel,
            Category = category,
            Message = formatter(state, exception),
            Exception = exception?.ToString(),
            Timestamp = DateTime.Now
        });
    }
}
