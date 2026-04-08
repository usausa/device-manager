namespace DeviceManager.Shared.Models;

public sealed class DeviceStatusReport
{
    public int Level { get; init; }
    public double Progress { get; init; }
    public double? Progress1 { get; init; }
    public double? Progress2 { get; init; }
    public int? Battery { get; init; }
    public int? WifiRssi { get; init; }
    public double? CpuUsage { get; init; }
    public double? MemoryUsage { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public IDictionary<string, double>? ProgressValues { get; init; }
    public IDictionary<string, string>? CustomData { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
