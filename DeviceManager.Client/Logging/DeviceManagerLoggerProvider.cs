namespace DeviceManager.Client.Logging;

using DeviceManager.Client.Telemetry;

// 端末ログをテレメトリ(gRPC)経由でサーバーへ転送する ILoggerProvider
public sealed class DeviceManagerLoggerProvider : ILoggerProvider
{
    private readonly TelemetryOptions options;

    private readonly DeviceManagerTelemetry telemetry;

    public DeviceManagerLoggerProvider(DeviceManagerClientOptions options, DeviceManagerTelemetry telemetry)
    {
        this.options = options.Telemetry;
        this.telemetry = telemetry;
    }

    public ILogger CreateLogger(string categoryName) =>
        new DeviceManagerLogger(categoryName, options, telemetry);

    public void Dispose()
    {
        // テレメトリ本体の破棄は所有者(生成側)が行う
    }
}
