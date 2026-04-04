namespace DeviceManager.Client.Sdk;

using DeviceManager.Shared.Models;

public interface IDeviceCommandHandler
{
    ValueTask<CommandResult> HandleCommandAsync(string command, string? payload, CancellationToken cancellationToken = default);
}
