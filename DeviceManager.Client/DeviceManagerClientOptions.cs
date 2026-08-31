namespace DeviceManager.Client;

using DeviceManager.Client.Telemetry;

#pragma warning disable CA1056
public sealed class DeviceManagerClientOptions
{
    // サーバーの URL(例: http://localhost:8082/)。SignalR / REST で使用
    public string ServerUrl { get; set; } = default!;

    // 共有 API キー(X-Api-Key)
    public string ApiKey { get; set; } = default!;

    // 自動再接続を行うか
    public bool AutoReconnect { get; set; } = true;

    // 再接続間隔の上限(指数バックオフの頭打ち)
    public int MaxReconnectIntervalSeconds { get; set; } = 60;

    // ステータス自動報告の既定間隔
    public int StatusIntervalSeconds { get; set; } = 30;

    // REST API / gRPC のタイムアウト
    public int ApiTimeoutSeconds { get; set; } = 30;

    // テレメトリ(メトリクス / ログ / クラッシュレポート)の設定
    public TelemetryOptions Telemetry { get; } = new();
}
#pragma warning restore CA1056
