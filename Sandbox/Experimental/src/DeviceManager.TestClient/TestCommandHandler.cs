using DeviceManager.Client.Sdk;
using DeviceManager.Shared.Models;

namespace DeviceManager.TestClient;

public class TestCommandHandler : IDeviceCommandHandler
{
    public event EventHandler<(string Command, string? Payload)>? CommandReceived;

    public ValueTask<CommandResult> HandleCommandAsync(string command, string? payload, CancellationToken cancellationToken = default)
    {
        CommandReceived?.Invoke(this, (command, payload));
        return ValueTask.FromResult(new CommandResult
        {
            CommandId = Guid.NewGuid().ToString("N"),
            Success = true,
            Result = $"Command '{command}' handled by WPF test client"
        });
    }
}
