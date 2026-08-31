namespace DeviceManager.Contracts;

/// <summary>
/// 端末通信ハブの定数。
/// </summary>
#pragma warning disable CA1034
public static class DeviceHubConstants
{
    /// <summary>ハブのパス。</summary>
    public const string Path = "/hubs/device";

    /// <summary>端末からサーバーへのメソッド名(テレメトリは gRPC を使用)。</summary>
    public static class Methods
    {
        /// <summary>端末登録。</summary>
        public const string Register = nameof(Register);

        /// <summary>メッセージ送信。</summary>
        public const string SendMessage = nameof(SendMessage);
    }

    /// <summary>サーバーから端末へのコールバック名。</summary>
    public static class Callbacks
    {
        /// <summary>メッセージ受信。</summary>
        public const string ReceiveMessage = nameof(ReceiveMessage);

        /// <summary>コンフィグ再読込。</summary>
        public const string ConfigReload = nameof(ConfigReload);
    }
}
#pragma warning restore CA1034

/// <summary>
/// API 共通の定数。
/// </summary>
public static class ApiConstants
{
    /// <summary>API キーのヘッダ名。</summary>
    public const string ApiKeyHeader = "X-Api-Key";
}
