using DeviceManager.Client.Sdk;
using DeviceManager.Shared.Models;

namespace DeviceManager.TestClient;

internal sealed class TestStatusProvider : IDeviceStatusProvider
{
    private readonly Random random = new();
    private int level;
    private double progress;

    public ValueTask<DeviceStatusReport> GetCurrentStatusAsync(CancellationToken cancellationToken = default)
    {
        level = (level + 1) % 10;
        progress = Math.Clamp(progress + random.NextDouble() * 5 - 1, 0, 100);

        var report = new DeviceStatusReport
        {
            Level = level,
            Progress = progress,
            Battery = Math.Max(0, 100 - level * 5 + random.Next(-5, 5)),
            WifiRssi = random.Next(-90, -30),
            Latitude = 35.6812 + (random.NextDouble() - 0.5) * 0.01,
            Longitude = 139.7671 + (random.NextDouble() - 0.5) * 0.01,
            Timestamp = DateTime.UtcNow
        };

        return ValueTask.FromResult(report);
    }
}
