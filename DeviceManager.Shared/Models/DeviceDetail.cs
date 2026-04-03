namespace DeviceManager.Shared.Models;

public sealed class DeviceDetail
{
    public required string DeviceId { get; init; }
    public required string Name { get; init; }
    public string? Platform { get; init; }
    public string? Group { get; init; }
    public string[]? Tags { get; init; }
    public string? Note { get; init; }
    public DeviceConnectionStatus Status { get; init; }
    public bool IsEnabled { get; init; }
    public int Level { get; init; }
    public double Progress { get; init; }
    public int? Battery { get; init; }
    public int? WifiRssi { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public IDictionary<string, double>? ProgressValues { get; init; }
    public IDictionary<string, string>? CustomData { get; init; }
    public DateTime RegisteredAt { get; init; }
    public DateTime? LastConnectedAt { get; init; }
    public DateTime? StatusTimestamp { get; init; }
}
