namespace DeviceManager.Shared.Models;

public sealed class DeviceSummary
{
    public required string DeviceId { get; init; }
    public required string Name { get; init; }
    public string? Group { get; init; }
    public DeviceConnectionStatus Status { get; init; }
    public int Level { get; init; }
    public double Progress { get; init; }
    public int? Battery { get; init; }
    public DateTime? LastConnectedAt { get; init; }
    public DateTime? StatusTimestamp { get; init; }
}
