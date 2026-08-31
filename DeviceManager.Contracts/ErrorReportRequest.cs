namespace DeviceManager.Contracts;

/// <summary>
/// エラーレポートの送信リクエスト。
/// </summary>
public sealed class ErrorReportRequest
{
    /// <summary>端末ID。</summary>
    [Required]
    [MaxLength(64)]
    public string DeviceId { get; set; } = default!;

    /// <summary>例外種別。</summary>
    [Required]
    [MaxLength(200)]
    public string ExceptionType { get; set; } = default!;

    /// <summary>メッセージ。</summary>
    [Required]
    public string Message { get; set; } = default!;

    /// <summary>スタックトレース。</summary>
    public string? StackTrace { get; set; }

    /// <summary>内部例外。</summary>
    public string? InnerException { get; set; }

    /// <summary>アプリバージョン。</summary>
    [MaxLength(50)]
    public string? AppVersion { get; set; }

    /// <summary>OSバージョン。</summary>
    [MaxLength(100)]
    public string? OsVersion { get; set; }

    /// <summary>発生日時。</summary>
    public DateTime OccurredAt { get; set; }
}
