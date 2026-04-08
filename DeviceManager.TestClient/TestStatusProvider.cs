namespace DeviceManager.TestClient;

using DeviceManager.Client.Sdk;
using DeviceManager.Shared.Models;

internal sealed class TestStatusProvider : IDeviceStatusProvider
{
    private readonly Random random = new();
    private int level;
    private double progress;
    private double progress2;

    public ValueTask<DeviceStatusReport> GetCurrentStatusAsync(CancellationToken cancellationToken = default)
    {
        level = (level + 1) % 10;
        progress = Math.Clamp(progress + (random.NextDouble() * 5) - 1, 0, 100);
        progress2 = Math.Clamp(progress2 + (random.NextDouble() * 4) - 1.5, 0, 100);

        var report = new DeviceStatusReport
        {
            Level = level,
            Progress = progress,
            Progress1 = progress,
            Progress2 = progress2,
            Battery = Math.Max(0, 100 - (level * 5) + random.Next(-5, 5)),
            WifiRssi = random.Next(-90, -30),
            CpuUsage = Math.Clamp(20 + (random.NextDouble() * 60), 0, 100),
            MemoryUsage = Math.Clamp(30 + (random.NextDouble() * 50), 0, 100),
            Latitude = 35.6812 + ((random.NextDouble() - 0.5) * 0.01),
            Longitude = 139.7671 + ((random.NextDouble() - 0.5) * 0.01),
            Timestamp = DateTime.UtcNow
        };

        return ValueTask.FromResult(report);
    }
}
