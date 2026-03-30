using System.Collections.Concurrent;
using DeviceManager.Server.Core.Services;
using DeviceManager.Shared;
using DeviceManager.Shared.Models;
using Microsoft.AspNetCore.SignalR;

namespace DeviceManager.Server.Web.Hubs;

public sealed class DeviceHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> ConnectionToDevice = new();

    private readonly DeviceService _deviceService;
    private readonly ConfigService _configService;
    private readonly MessageService _messageService;
    private readonly ILogger<DeviceHub> _logger;

    public DeviceHub(
        DeviceService deviceService,
        ConfigService configService,
        MessageService messageService,
        ILogger<DeviceHub> logger)
    {
        _deviceService = deviceService;
        _configService = configService;
        _messageService = messageService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionToDevice.TryRemove(Context.ConnectionId, out var deviceId))
        {
            await _deviceService.UpdateConnectionStatusAsync(deviceId, DeviceConnectionStatus.Inactive);

            await Clients.Group(HubConstants.Groups.Dashboard)
                .SendAsync(HubConstants.DashboardMethods.DeviceDisconnected, deviceId);

            _logger.LogInformation("Device disconnected: {DeviceId} ({ConnectionId})", deviceId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task Register(DeviceRegistration registration)
    {
        await _deviceService.RegisterDeviceAsync(registration);
        await _deviceService.UpdateConnectionStatusAsync(registration.DeviceId, DeviceConnectionStatus.Active);

        ConnectionToDevice[Context.ConnectionId] = registration.DeviceId;

        // Add to device-specific group
        await Groups.AddToGroupAsync(Context.ConnectionId, HubConstants.Groups.Device(registration.DeviceId));

        // Add to device group if specified
        if (!string.IsNullOrEmpty(registration.Group))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, HubConstants.Groups.DeviceGroup(registration.Group));
        }

        // Notify dashboard
        await Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(HubConstants.DashboardMethods.DeviceConnected, registration);

        // Send config reload to the device
        var config = await _configService.GetResolvedConfigAsync(registration.DeviceId);
        await Clients.Caller.SendAsync(HubConstants.ServerMethods.ConfigReload, config);

        _logger.LogInformation("Device registered: {DeviceId} ({Name})", registration.DeviceId, registration.Name);
    }

    public async Task ReportStatus(DeviceStatusReport report)
    {
        if (!ConnectionToDevice.TryGetValue(Context.ConnectionId, out var deviceId))
        {
            _logger.LogWarning("Status report from unregistered connection: {ConnectionId}", Context.ConnectionId);
            return;
        }

        await _deviceService.UpdateStatusAsync(deviceId, report);

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

        await _messageService.AddMessageAsync(message);

        await Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(HubConstants.DashboardMethods.MessageReceived, message);
    }

    public async Task CommandResult(string commandId, bool success, string? result)
    {
        ConnectionToDevice.TryGetValue(Context.ConnectionId, out var deviceId);

        var commandResult = new Shared.Models.CommandResult
        {
            CommandId = commandId,
            Success = success,
            Result = result
        };

        _logger.LogInformation("Command result from {DeviceId}: {CommandId} Success={Success}",
            deviceId, commandId, success);

        await Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(nameof(CommandResult), commandResult);
    }
}
