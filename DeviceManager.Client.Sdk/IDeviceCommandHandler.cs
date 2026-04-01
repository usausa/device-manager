using DeviceManager.Shared.Models;

namespace DeviceManager.Client.Sdk;

public interface IDeviceCommandHandler
{
    ValueTask<CommandResult> HandleCommandAsync(string command, string? payload, CancellationToken cancellationToken = default);
}
