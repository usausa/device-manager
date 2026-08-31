namespace DeviceManager.Server.Models.Forms;

public sealed class MessageForm
{
    // null / 空は全体送信
    public string? DeviceId { get; set; }

    public string MessageType { get; set; } = "text";

    public string Content { get; set; } = string.Empty;
}
