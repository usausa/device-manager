namespace DeviceManager.Shared.Models;

public sealed class SendMessageRequest
{
    public string? DeviceId { get; init; }
    public required string MessageType { get; init; }
    public required string Content { get; init; }
}

public sealed class SendCommandRequest
{
    public required string DeviceId { get; init; }
    public required string Command { get; init; }
    public string? Payload { get; init; }
}
