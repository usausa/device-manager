namespace DeviceMonitor.Server.Models;

public class StatusEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    public DateTime Timestamp { get; set; }

    public double Battery { get; set; }

    public double? Longitude { get; set; }

    public double? Latitude { get; set; }

    public DateTime? LastLocationAt { get; set; }
}
