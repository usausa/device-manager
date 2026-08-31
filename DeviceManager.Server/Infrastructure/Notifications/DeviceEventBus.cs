namespace DeviceManager.Server.Infrastructure.Notifications;

public enum DeviceEventType
{
    Connected,
    Disconnected,
    StatusUpdated,
    MessageReceived,
    LogReceived,
    ErrorReportReceived
}

public sealed class DeviceEventArgs : EventArgs
{
    public DeviceEventType Type { get; }

    public string? DeviceId { get; }

    public DeviceEventArgs(DeviceEventType type, string? deviceId)
    {
        Type = type;
        DeviceId = deviceId;
    }
}

// プロセス内イベントバス(端末イベントを管理画面へリアルタイム反映するために使用)
public sealed class DeviceEventBus
{
    public event EventHandler<DeviceEventArgs>? Received;

    public void Publish(DeviceEventType type, string? deviceId = null) =>
        Received?.Invoke(this, new DeviceEventArgs(type, deviceId));
}
