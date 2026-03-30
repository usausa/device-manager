using DeviceManager.Shared.Models;

namespace DeviceManager.Client.Sdk;

public interface IDeviceStatusProvider
{
    ValueTask<DeviceStatusReport> GetCurrentStatusAsync(CancellationToken cancellationToken = default);
}
