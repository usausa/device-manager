using DeviceManager.Client.Sdk;
using DeviceManager.Shared.Models;

namespace DeviceManager.TestClient;

internal sealed class TestStatusProvider : IDeviceStatusProvider
{
    private readonly Random _random = new();
    private int _level;
    private double _progress;

    public ValueTask<DeviceStatusReport> GetCurrentStatusAsync(CancellationToken cancellationToken = default)
    {
        _level = (_level + 1) % 10;
        _progress = Math.Clamp(_progress + _random.NextDouble() * 5 - 1, 0, 100);

        var report = new DeviceStatusReport
        {
            Level = _level,
            Progress = _progress,
            Battery = Math.Max(0, 100 - _level * 5 + _random.Next(-5, 5)),
            Latitude = 35.6812 + (_random.NextDouble() - 0.5) * 0.01,
            Longitude = 139.7671 + (_random.NextDouble() - 0.5) * 0.01,
            Timestamp = DateTime.UtcNow
        };

        return ValueTask.FromResult(report);
    }
}
