namespace DeviceManager.Client.Telemetry;

using Microsoft.Extensions.Logging.Abstractions;

// テレメトリ(メトリクス / ログ / クラッシュレポート)の送信窓口。
// すべて非同期のバックグラウンド送信で、送信エラー時はタンキングプロバイダーへ退避して自動再送する
public sealed class DeviceManagerTelemetry : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDeviceInfoProvider infoProvider;

    private readonly ITelemetryStore store;

    private readonly TelemetryGrpcTransport transport;

    private readonly TelemetryChannel channel;

    public DeviceManagerTelemetry(
        DeviceManagerClientOptions options,
        IDeviceInfoProvider infoProvider,
        ILogger? log = null)
    {
        this.infoProvider = infoProvider;

        // タンキングプロバイダーは設定から生成(プロバイダーモデル)
        store = options.Telemetry.Store.CreateStore();
        transport = new TelemetryGrpcTransport(options, infoProvider);
        channel = new TelemetryChannel(options.Telemetry, store, transport, log ?? NullLogger.Instance);
    }

    public async ValueTask DisposeAsync()
    {
        await channel.DisposeAsync().ConfigureAwait(false);
        transport.Dispose();
        store.Dispose();
    }

    //--------------------------------------------------------------------------------
    // Metric
    //--------------------------------------------------------------------------------

    // メトリクス(ステータス)の送信(非ブロッキング)
    public void ReportStatus(DeviceStatusReport report) =>
        channel.Enqueue(TelemetryKind.Metric, JsonSerializer.Serialize(report, JsonOptions));

    //--------------------------------------------------------------------------------
    // Log
    //--------------------------------------------------------------------------------

    // ログの送信(非ブロッキング。通常は DeviceManagerLoggerProvider 経由で使用)
    public void SendLog(DeviceLogRecord record) =>
        channel.Enqueue(TelemetryKind.Log, JsonSerializer.Serialize(record, JsonOptions));

    //--------------------------------------------------------------------------------
    // Crash report
    //--------------------------------------------------------------------------------

    // クラッシュレポートの送信(非ブロッキング。即時フラッシュを要求する)
    public void ReportCrash(ErrorReportRequest request)
    {
        if (String.IsNullOrEmpty(request.DeviceId))
        {
            request.DeviceId = infoProvider.DeviceId;
        }

        channel.Enqueue(TelemetryKind.CrashReport, JsonSerializer.Serialize(request, JsonOptions), urgent: true);
    }

    public void ReportCrash(Exception exception, string? appVersion = null, string? osVersion = null) =>
        ReportCrash(new ErrorReportRequest
        {
            DeviceId = infoProvider.DeviceId,
            ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            InnerException = exception.InnerException?.ToString(),
            AppVersion = appVersion,
            OsVersion = osVersion,
            OccurredAt = DateTime.Now
        });

    //--------------------------------------------------------------------------------
    // Control
    //--------------------------------------------------------------------------------

    // 未送信件数(キュー + タンク)
    public ValueTask<int> GetPendingCountAsync(CancellationToken cancellationToken = default) =>
        channel.GetPendingCountAsync(cancellationToken);

    // 即時フラッシュ要求(バックオフ解除。ネットワーク復旧検知時などに使用)
    public void TriggerFlush() =>
        channel.TriggerFlush();
}
