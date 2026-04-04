namespace DeviceManager.TestClient;

using DeviceManager.Client.Sdk;
using DeviceManager.Shared.Models;

internal sealed class TestCommandHandler : IDeviceCommandHandler
{
    public ValueTask<CommandResult> HandleCommandAsync(string command, string? payload, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"  📥 Command received: {command} (payload: {payload ?? "none"})");

        var result = command.ToLowerInvariant() switch
        {
            "ping" => new CommandResult { CommandId = string.Empty, Success = true, Result = "pong" },
            "echo" => new CommandResult { CommandId = string.Empty, Success = true, Result = payload },
            "restart" => new CommandResult { CommandId = string.Empty, Success = true, Result = "Restart simulated" },
            _ => new CommandResult { CommandId = string.Empty, Success = false, Result = $"Unknown command: {command}" }
        };

        return ValueTask.FromResult(result);
    }
}
