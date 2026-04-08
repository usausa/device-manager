namespace DeviceManager.Shared.Models;

/// <summary>A device connect or disconnect event recorded in the connection log.</summary>
public sealed class ConnectionEvent
{
    public long Id { get; init; }
    public required string DeviceId { get; init; }
    public bool Connected { get; init; }
    public DateTime Timestamp { get; init; }
}
