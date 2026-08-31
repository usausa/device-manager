namespace DeviceManager.Client.Telemetry;

// テレメトリの種別
public enum TelemetryKind
{
    // メトリクス(ステータス)
    Metric = 0,

    // ログ
    Log = 1,

    // クラッシュレポート
    CrashReport = 2
}

// タンキング対象のテレメトリ 1 件(Payload は各 DTO の JSON)
public sealed class TelemetryEnvelope
{
    // ストアが採番する識別子
    public long Id { get; set; }

    public TelemetryKind Kind { get; set; }

    public string Payload { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
}
