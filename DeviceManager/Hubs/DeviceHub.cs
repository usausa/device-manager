namespace DeviceManager.Hubs;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

public sealed class DeviceHub(
    DeviceService deviceService,
    ConfigService configService,
    MessageService messageService,
    LogService logService,
    ILogger<DeviceHub> logger) : Hub
{
    private static readonly ConcurrentDictionary<string, string> ConnectionToDevice = new();

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Allows dashboard clients to join notification groups.
    /// </summary>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionToDevice.TryRemove(Context.ConnectionId, out var deviceId))
        {
            await deviceService.UpdateConnectionStatusAsync(deviceId, DeviceConnectionStatus.Inactive);
            await Clients.Group(HubConstants.Groups.Dashboard)
                .SendAsync(HubConstants.DashboardMethods.DeviceDisconnected, deviceId);
            logger.LogInformation("Device disconnected: {DeviceId} ({ConnectionId})", deviceId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task Register(DeviceRegistration registration)
    {
        await deviceService.RegisterDeviceAsync(registration);
        await deviceService.UpdateConnectionStatusAsync(registration.DeviceId, DeviceConnectionStatus.Active);

        ConnectionToDevice[Context.ConnectionId] = registration.DeviceId;

        await Groups.AddToGroupAsync(Context.ConnectionId, HubConstants.Groups.Device(registration.DeviceId));
        if (!string.IsNullOrEmpty(registration.Group))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, HubConstants.Groups.DeviceGroup(registration.Group));
        }

        await Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(HubConstants.DashboardMethods.DeviceConnected, registration);

        var config = await configService.GetResolvedConfigAsync(registration.DeviceId);
        await Clients.Caller.SendAsync(HubConstants.ServerMethods.ConfigReload, config);

        logger.LogInformation("Device registered: {DeviceId} ({Name})", registration.DeviceId, registration.Name);
    }

    public async Task ReportStatus(DeviceStatusReport report)
    {
        if (!ConnectionToDevice.TryGetValue(Context.ConnectionId, out var deviceId))
        {
            logger.LogWarning("Status report from unregistered connection: {ConnectionId}", Context.ConnectionId);
            return;
        }

        await deviceService.UpdateStatusAsync(deviceId, report);
        await Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(HubConstants.DashboardMethods.DeviceStatusUpdated, deviceId, report);
    }

    public async Task SendMessage(string messageType, string content)
    {
        ConnectionToDevice.TryGetValue(Context.ConnectionId, out var deviceId);

        var message = new ServerMessage
        {
            DeviceId = deviceId,
            Direction = MessageDirection.DeviceToServer,
            MessageType = messageType,
            Content = content,
            Status = MessageStatus.Delivered,
            CreatedAt = DateTime.UtcNow
        };

        await messageService.AddMessageAsync(message);
        await Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(HubConstants.DashboardMethods.MessageReceived, message);
    }

    public async Task SendLog(LogEntry entry)
    {
        if (!ConnectionToDevice.TryGetValue(Context.ConnectionId, out var deviceId))
        {
            return;
        }

        var logEntry = new LogEntry
        {
            DeviceId = deviceId,
            Level = entry.Level,
            Category = entry.Category,
            Message = entry.Message,
            Exception = entry.Exception,
            Timestamp = entry.Timestamp
        };

        await logService.AddLogEntryAsync(logEntry);
        await Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(HubConstants.DashboardMethods.LogReceived, logEntry);
    }

    public async Task CommandResult(string commandId, bool success, string? result)
    {
        ConnectionToDevice.TryGetValue(Context.ConnectionId, out var deviceId);
        logger.LogInformation("Command result from {DeviceId}: {CommandId} Success={Success}", deviceId, commandId, success);

        var commandResult = new Shared.Models.CommandResult
        {
            CommandId = commandId,
            Success = success,
            Result = result
        };

        await Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(nameof(CommandResult), commandResult);
    }
}
