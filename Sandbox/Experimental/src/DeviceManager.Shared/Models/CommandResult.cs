namespace DeviceManager.Shared.Models;

public sealed class CommandResult
{
    public required string CommandId { get; init; }
    public bool Success { get; init; }
    public string? Result { get; init; }
}
