namespace DeviceManager.Contracts;

/// <summary>
/// 端末ログのレコード。
/// </summary>
public sealed class DeviceLogRecord
{
    /// <summary>レベル。</summary>
    public DeviceLogLevel Level { get; set; }

    /// <summary>カテゴリ。</summary>
    [Required]
    [MaxLength(200)]
    public string Category { get; set; } = default!;

    /// <summary>メッセージ。</summary>
    [Required]
    public string Message { get; set; } = default!;

    /// <summary>例外情報。</summary>
    public string? Exception { get; set; }

    /// <summary>発生日時。</summary>
    public DateTime Timestamp { get; set; }
}
