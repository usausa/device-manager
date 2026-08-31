namespace DeviceManager.Client.Telemetry;

// タンキングプロバイダーの設定基底。派生クラスがプロバイダー実装を生成する(プロバイダーモデル)
public abstract class TelemetryStoreOptions
{
    // 保持する最大件数(超過時は古いものから破棄)
    public int MaxItems { get; set; } = 10000;

    // 保持期間(時間)。超過したものは再送されず破棄される
    public int RetentionHours { get; set; } = 72;

    // プロバイダーの生成
    public abstract ITelemetryStore CreateStore();
}

// オンメモリのタンキング(プロセス終了で消える)
public sealed class MemoryTelemetryStoreOptions : TelemetryStoreOptions
{
    public override ITelemetryStore CreateStore() => new MemoryTelemetryStore(this);
}

// SQLite によるタンキング(プロセス再起動後も再送可能)
public sealed class SqliteTelemetryStoreOptions : TelemetryStoreOptions
{
    // データベースファイルのパス
    public string FilePath { get; set; } = "telemetry.db";

    public override ITelemetryStore CreateStore() => new SqliteTelemetryStore(this);
}
