namespace DeviceManager.Contracts;

/// <summary>
/// 端末とサーバー間のメッセージ。
/// </summary>
public sealed class DeviceMessage
{
    /// <summary>メッセージID。</summary>
    public long MessageId { get; set; }

    /// <summary>対象端末ID(null は全体送信)。</summary>
    public string? DeviceId { get; set; }

    /// <summary>メッセージ種別。</summary>
    public string MessageType { get; set; } = default!;

    /// <summary>内容。</summary>
    public string Content { get; set; } = default!;

    /// <summary>作成日時。</summary>
    public DateTime Timestamp { get; set; }
}
