namespace DeviceManager.Shared.Models;

public sealed class ServerMessage
{
    public long MessageId { get; init; }
    public string? DeviceId { get; init; }
    public MessageDirection Direction { get; init; }
    public required string MessageType { get; init; }
    public required string Content { get; init; }
    public MessageStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
}
