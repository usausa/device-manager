namespace DeviceManager.Server.Models.Message;

public sealed record MessageSendRequest(
    string? DeviceId,
    [property: Required][property: MaxLength(50)] string MessageType,
    [property: Required][property: MaxLength(2000)] string Content);
