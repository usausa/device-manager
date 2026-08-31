namespace DeviceManager.Server.Settings;

public sealed class CleanupSetting
{
    public bool Enable { get; set; }

    [Range(60, 86400)]
    public int IntervalSeconds { get; set; } = 3600;

    // 履歴系(ステータス履歴・接続ログ・端末ログ・エラーレポート)の保持日数
    [Range(1, 3650)]
    public int RetentionDays { get; set; } = 30;
}
