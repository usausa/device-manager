namespace DeviceManager.Models.Entity;

public sealed class MessageEntity
{
    public long MessageId { get; set; }

    // null は全体送信
    public string? DeviceId { get; set; }

    // MessageDirection の値
    public int Direction { get; set; }

    public string MessageType { get; set; } = default!;

    public string Content { get; set; } = default!;

    // MessageDeliveryStatus の値
    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }
}
