namespace DeviceManager.Client.Telemetry;

#pragma warning disable CA1056
public sealed class TelemetryOptions
{
    // gRPC エンドポイントの URL(例: http://localhost:8083/。HTTP/2 専用ポート)
    public string GrpcUrl { get; set; } = default!;

    // 送信待ちキューの上限(超過時は古いものから破棄)
    public int QueueSize { get; set; } = 1000;

    // 1 回の送信件数
    public int BatchSize { get; set; } = 100;

    // 送信間隔
    public int FlushIntervalSeconds { get; set; } = 5;

    // 送信失敗時のバックオフ上限
    public int MaxBackoffSeconds { get; set; } = 300;

    // ログ転送のレベル下限
    public DeviceLogLevel LogMinLevel { get; set; } = DeviceLogLevel.Information;

    // タンキングプロバイダーの設定(既定はオンメモリ。SqliteTelemetryStoreOptions で永続化)
    public TelemetryStoreOptions Store { get; set; } = new MemoryTelemetryStoreOptions();
}
#pragma warning restore CA1056
