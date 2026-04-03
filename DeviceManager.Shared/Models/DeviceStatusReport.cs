namespace DeviceManager.Shared.Models;

public sealed class DeviceStatusReport
{
    public int Level { get; init; }
    public double Progress { get; init; }
    public int? Battery { get; init; }
    public int? WifiRssi { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public IDictionary<string, double>? ProgressValues { get; init; }
    public IDictionary<string, string>? CustomData { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
