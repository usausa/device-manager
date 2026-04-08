namespace DeviceManager.Services;

using System.Collections.Concurrent;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Integrates device connection tracking (formerly in DeviceHub) and the in-process event bus
/// (formerly AppEventBus) into a single, centrally managed service.
///
/// Responsibilities:
///   - Track SignalR connection → DeviceId mappings.
///   - Handle device lifecycle events from both SignalR (Handle*Async) and gRPC (Notify*Async).
///   - Broadcast real-time updates to the dashboard group over SignalR.
///   - Publish in-process events to subscribed Blazor Server components.
///   - Provide outgoing helpers for the REST API (SendToDevice, BroadcastToDevices, SendCommand).
/// </summary>
public sealed class DeviceEventService(
    IHubContext<DeviceHub> hubContext,
    DeviceService deviceService,
    ConfigService configService,
    MessageService messageService,
    LogService logService,
    CrashReportService crashReportService,
    ScreenshotStore screenshotStore,
    ILogger<DeviceEventService> logger)
{
    // ── Connection tracking ────────────────────────────────────────────────────
    private readonly ConcurrentDictionary<string, string> connectionToDevice = new();

    // ── In-process pub/sub ─────────────────────────────────────────────────────
    private readonly ConcurrentDictionary<string, List<Func<object?, Task>>> handlers = new();

    // ── Pub/Sub ────────────────────────────────────────────────────────────────

    /// <summary>Subscribes to a named in-process application event.</summary>
    public IDisposable Subscribe(string eventName, Func<object?, Task> handler)
    {
        var list = handlers.GetOrAdd(eventName, _ => []);
        lock (list)
        {
            list.Add(handler);
        }

        return new Subscription(this, eventName, handler);
    }

    /// <summary>Publishes a named in-process event to all registered subscribers.</summary>
    public async Task PublishAsync(string eventName, object? payload = null)
    {
        if (!handlers.TryGetValue(eventName, out var list))
        {
            return;
        }

        Func<object?, Task>[] snapshot;
        lock (list)
        {
            snapshot = [.. list];
        }

        foreach (var handler in snapshot)
        {
            await handler(payload);
        }
    }

    // ── SignalR hub delegates ──────────────────────────────────────────────────

    /// <summary>Called by DeviceHub when a dashboard client joins a notification group.</summary>
    public Task HandleGroupJoinAsync(string connectionId, string groupName)
        => hubContext.Groups.AddToGroupAsync(connectionId, groupName);

    /// <summary>Called by DeviceHub.OnDisconnectedAsync when a SignalR connection closes.</summary>
    public async Task HandleSignalRDisconnectedAsync(string connectionId)
    {
        if (!connectionToDevice.TryRemove(connectionId, out var deviceId))
        {
            return;
        }

        await deviceService.UpdateConnectionStatusAsync(deviceId, DeviceConnectionStatus.Inactive);
        await deviceService.LogConnectionEventAsync(deviceId, connected: false);
        await NotifyDeviceDisconnectedAsync(deviceId);
        logger.LogInformation("Device disconnected: {DeviceId} ({ConnectionId})", deviceId, connectionId);
    }

    /// <summary>Called by DeviceHub.Register to handle a device registering over SignalR.</summary>
    public async Task HandleSignalRRegisterAsync(string connectionId, DeviceRegistration registration)
    {
        await deviceService.RegisterDeviceAsync(registration);
        await deviceService.UpdateConnectionStatusAsync(registration.DeviceId, DeviceConnectionStatus.Active);
        await deviceService.LogConnectionEventAsync(registration.DeviceId, connected: true);

        connectionToDevice[connectionId] = registration.DeviceId;

        await hubContext.Groups.AddToGroupAsync(connectionId, HubConstants.Groups.Device(registration.DeviceId));
        if (!string.IsNullOrEmpty(registration.Group))
        {
            await hubContext.Groups.AddToGroupAsync(connectionId, HubConstants.Groups.DeviceGroup(registration.Group));
        }

        await NotifyDeviceConnectedAsync(registration);

        var config = await configService.GetResolvedConfigAsync(registration.DeviceId);
        await hubContext.Clients.Client(connectionId).SendAsync(HubConstants.ServerMethods.ConfigReload, config);

        logger.LogInformation("Device registered via SignalR: {DeviceId} ({Name})", registration.DeviceId, registration.Name);
    }

    /// <summary>Called by DeviceHub.ReportStatus to process a status report received over SignalR.</summary>
    public async Task HandleSignalRStatusReportAsync(string connectionId, DeviceStatusReport report)
    {
        if (!connectionToDevice.TryGetValue(connectionId, out var deviceId))
        {
            logger.LogWarning("Status report from unregistered SignalR connection: {ConnectionId}", connectionId);
            return;
        }

        await deviceService.UpdateStatusAsync(deviceId, report);
        await NotifyStatusUpdatedAsync(deviceId, report);
    }

    /// <summary>Called by DeviceHub.SendMessage to process a device-to-server message received over SignalR.</summary>
    public async Task HandleSignalRMessageAsync(string connectionId, string messageType, string content)
    {
        connectionToDevice.TryGetValue(connectionId, out var deviceId);

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
        await NotifyMessageReceivedAsync(message);
    }

    /// <summary>Called by DeviceHub.SendLog to store and broadcast a log entry received over SignalR.</summary>
    public async Task HandleSignalRLogAsync(string connectionId, LogEntry entry)
    {
        if (!connectionToDevice.TryGetValue(connectionId, out var deviceId))
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
        await NotifyLogReceivedAsync(logEntry);
    }

    /// <summary>Called by DeviceHub.CommandResult to handle a command execution result from a device.</summary>
    public async Task HandleSignalRCommandResultAsync(
        string connectionId, string commandId, bool success, string? result)
    {
        connectionToDevice.TryGetValue(connectionId, out var deviceId);
        logger.LogInformation(
            "Command result from {DeviceId}: {CommandId} Success={Success}", deviceId, commandId, success);

        var commandResult = new CommandResult
        {
            CommandId = commandId,
            Success = success,
            Result = result
        };

        await hubContext.Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(nameof(DeviceHub.CommandResult), commandResult);
    }

    // ── Notifications (shared by SignalR and gRPC paths) ──────────────────────

    /// <summary>Notifies the dashboard and in-process subscribers that a device has connected.</summary>
    public async Task NotifyDeviceConnectedAsync(DeviceRegistration registration)
    {
        await hubContext.Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(HubConstants.DashboardMethods.DeviceConnected, registration);
        await PublishAsync(AppEvents.DeviceConnected, registration.DeviceId);
    }

    /// <summary>Notifies the dashboard and in-process subscribers that a device has disconnected.</summary>
    public async Task NotifyDeviceDisconnectedAsync(string deviceId)
    {
        await hubContext.Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(HubConstants.DashboardMethods.DeviceDisconnected, deviceId);
        await PublishAsync(AppEvents.DeviceDisconnected, deviceId);
    }

    /// <summary>Notifies the dashboard and in-process subscribers that a device status was updated.</summary>
    public async Task NotifyStatusUpdatedAsync(string deviceId, DeviceStatusReport report)
    {
        await hubContext.Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(HubConstants.DashboardMethods.DeviceStatusUpdated, deviceId, report);
        await PublishAsync(AppEvents.DeviceStatusUpdated, deviceId);
    }

    /// <summary>Notifies the dashboard and in-process subscribers that a message was received from a device.</summary>
    public async Task NotifyMessageReceivedAsync(ServerMessage message)
    {
        await hubContext.Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(HubConstants.DashboardMethods.MessageReceived, message);
        await PublishAsync(AppEvents.MessageReceived, message);
    }

    /// <summary>Notifies the dashboard and in-process subscribers that a log entry was received from a device.</summary>
    public async Task NotifyLogReceivedAsync(LogEntry entry)
    {
        await hubContext.Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(HubConstants.DashboardMethods.LogReceived, entry);
        await PublishAsync(AppEvents.LogReceived, entry);
    }

    /// <summary>Called by DeviceHub.SendCrashReport to process a crash report received over SignalR.</summary>
    public async Task HandleSignalRCrashReportAsync(string connectionId, CrashReport report)
    {
        if (!connectionToDevice.TryGetValue(connectionId, out var deviceId))
        {
            return;
        }

        var entry = new CrashReport
        {
            DeviceId = deviceId,
            ExceptionType = report.ExceptionType,
            Message = report.Message,
            StackTrace = report.StackTrace,
            InnerException = report.InnerException,
            AppVersion = report.AppVersion,
            OsVersion = report.OsVersion,
            AdditionalData = report.AdditionalData,
            OccurredAt = report.OccurredAt,
            ReceivedAt = DateTime.UtcNow
        };

        var saved = await crashReportService.AddReportAsync(entry);
        await NotifyCrashReportAsync(saved);
    }

    /// <summary>Notifies the dashboard and in-process subscribers that a crash report was received from a device.</summary>
    public async Task NotifyCrashReportAsync(CrashReport report)
    {
        await hubContext.Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(HubConstants.DashboardMethods.CrashReportReceived, report);
        await PublishAsync(AppEvents.CrashReportReceived, report);
    }

    // ── Screenshot ────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a screenshot request to a device over SignalR and returns the generated request ID.
    /// </summary>
    public async Task<string> RequestScreenshotAsync(string deviceId)
    {
        var requestId = Guid.NewGuid().ToString("N");
        await hubContext.Clients.Group(HubConstants.Groups.Device(deviceId))
            .SendAsync(HubConstants.ServerMethods.TakeScreenshot, requestId);
        logger.LogInformation("Screenshot requested for {DeviceId} (requestId={RequestId})", deviceId, requestId);
        return requestId;
    }

    /// <summary>Called by DeviceHub.UploadScreenshot when a device returns a captured screenshot.</summary>
    public async Task HandleSignalRScreenshotAsync(
        string connectionId, string requestId, string base64Data, string contentType)
    {
        if (!connectionToDevice.TryGetValue(connectionId, out var deviceId))
        {
            return;
        }

        var result = new ScreenshotResult
        {
            RequestId = requestId,
            DeviceId = deviceId,
            Base64Data = base64Data,
            ContentType = string.IsNullOrEmpty(contentType) ? "image/png" : contentType,
            CapturedAt = DateTime.UtcNow
        };

        screenshotStore.Save(result);

        await hubContext.Clients.Group(HubConstants.Groups.Dashboard)
            .SendAsync(HubConstants.DashboardMethods.ScreenshotReceived, result);
        await PublishAsync(AppEvents.ScreenshotReceived, result);
        logger.LogInformation("Screenshot received from {DeviceId} (requestId={RequestId})", deviceId, requestId);
    }

    /// <summary>Sends a message to a specific device over SignalR.</summary>
    public Task SendMessageToDeviceAsync(string deviceId, string messageType, string content)
        => hubContext.Clients.Group(HubConstants.Groups.Device(deviceId))
            .SendAsync(HubConstants.ServerMethods.ReceiveMessage, messageType, content);

    /// <summary>Broadcasts a message to all connected devices over SignalR.</summary>
    public Task BroadcastMessageToDevicesAsync(string messageType, string content)
        => hubContext.Clients.Group(HubConstants.Groups.AllDevices)
            .SendAsync(HubConstants.ServerMethods.ReceiveMessage, messageType, content);

    /// <summary>Sends a command to a specific device over SignalR.</summary>
    public Task SendCommandToDeviceAsync(string deviceId, string commandId, string command, string payload)
        => hubContext.Clients.Group(HubConstants.Groups.Device(deviceId))
            .SendAsync(HubConstants.ServerMethods.ReceiveCommand, commandId, command, payload);

    // ── Subscription cleanup ──────────────────────────────────────────────────

    private void Unsubscribe(string eventName, Func<object?, Task> handler)
    {
        if (!handlers.TryGetValue(eventName, out var list))
        {
            return;
        }

        lock (list)
        {
            list.Remove(handler);
        }
    }

    private sealed class Subscription(DeviceEventService service, string eventName, Func<object?, Task> handler)
        : IDisposable
    {
        public void Dispose() => service.Unsubscribe(eventName, handler);
    }
}
