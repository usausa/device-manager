using DeviceManager.Client.Sdk;
using DeviceManager.Shared.Models;

namespace DeviceManager.TestClient;

public class TestStatusProvider : IDeviceStatusProvider
{
    public int Level { get; set; }
    public double Progress { get; set; }
    public int? Battery { get; set; } = 100;

    public ValueTask<DeviceStatusReport> GetCurrentStatusAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new DeviceStatusReport
        {
            Level = Level,
            Progress = Progress,
            Battery = Battery,
            Timestamp = DateTime.UtcNow
        });
    }
}
