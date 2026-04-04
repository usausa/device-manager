namespace DeviceManager.Client.Sdk;

using DeviceManager.Shared.Models;

public interface IDeviceStatusProvider
{
    ValueTask<DeviceStatusReport> GetCurrentStatusAsync(CancellationToken cancellationToken = default);
}
