namespace DeviceManager.Services;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Thin SignalR hub that routes all device events to <see cref="DeviceEventService"/>.
/// Business logic, connection tracking, and dashboard notifications are handled there.
/// </summary>
public sealed class DeviceHub(DeviceEventService events, ILogger<DeviceHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await events.HandleSignalRDisconnectedAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Allows dashboard clients to join notification groups.</summary>
    public Task JoinGroup(string groupName)
        => events.HandleGroupJoinAsync(Context.ConnectionId, groupName);

    public Task Register(DeviceRegistration registration)
        => events.HandleSignalRRegisterAsync(Context.ConnectionId, registration);

    public Task ReportStatus(DeviceStatusReport report)
        => events.HandleSignalRStatusReportAsync(Context.ConnectionId, report);

    public Task SendMessage(string messageType, string content)
        => events.HandleSignalRMessageAsync(Context.ConnectionId, messageType, content);

    public Task SendLog(LogEntry entry)
        => events.HandleSignalRLogAsync(Context.ConnectionId, entry);

    public Task CommandResult(string commandId, bool success, string? result)
        => events.HandleSignalRCommandResultAsync(Context.ConnectionId, commandId, success, result);
}
