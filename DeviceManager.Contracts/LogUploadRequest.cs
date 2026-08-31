namespace DeviceManager.Contracts;

/// <summary>
/// 端末ログの一括送信リクエスト。
/// </summary>
#pragma warning disable CA1002
public sealed class LogUploadRequest
{
    /// <summary>端末ID。</summary>
    [Required]
    [MaxLength(64)]
    public string DeviceId { get; set; } = default!;

    /// <summary>ログレコード。</summary>
    [Required]
    public List<DeviceLogRecord> Records { get; init; } = default!;
}
#pragma warning restore CA1002
