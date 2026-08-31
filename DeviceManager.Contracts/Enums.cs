namespace DeviceManager.Contracts;

/// <summary>
/// 端末の状態。
/// </summary>
public enum DeviceState
{
    /// <summary>停止。</summary>
    Inactive = 0,

    /// <summary>稼働。</summary>
    Active = 1,

    /// <summary>警告。</summary>
    Warning = 2,

    /// <summary>異常。</summary>
    Error = 3
}

/// <summary>
/// 端末ログのレベル(Microsoft.Extensions.Logging.LogLevel と同値)。
/// </summary>
public enum DeviceLogLevel
{
    /// <summary>トレース。</summary>
    Trace = 0,

    /// <summary>デバッグ。</summary>
    Debug = 1,

    /// <summary>情報。</summary>
    Information = 2,

    /// <summary>警告。</summary>
    Warning = 3,

    /// <summary>エラー。</summary>
    Error = 4,

    /// <summary>致命的。</summary>
    Critical = 5
}

/// <summary>
/// メッセージの方向。
/// </summary>
public enum MessageDirection
{
    /// <summary>サーバーから端末。</summary>
    ServerToDevice = 0,

    /// <summary>端末からサーバー。</summary>
    DeviceToServer = 1
}

/// <summary>
/// メッセージの配送状態。
/// </summary>
public enum MessageDeliveryStatus
{
    /// <summary>送信済み。</summary>
    Sent = 0,

    /// <summary>配送済み。</summary>
    Delivered = 1,

    /// <summary>失敗。</summary>
    Failed = 2
}
