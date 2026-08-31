namespace DeviceManager.Server.Infrastructure.Devices;

using System.Collections.Concurrent;

// SignalR の接続と端末 ID の対応を追跡する
public sealed class DeviceConnectionRegistry
{
    private readonly ConcurrentDictionary<string, string> connectionToDevice = new();

    private readonly ConcurrentDictionary<string, string> deviceToConnection = new();

    public int Count => deviceToConnection.Count;

    public void Register(string connectionId, string deviceId)
    {
        connectionToDevice[connectionId] = deviceId;
        deviceToConnection[deviceId] = connectionId;
    }

    public string? ResolveDevice(string connectionId) =>
        connectionToDevice.TryGetValue(connectionId, out var deviceId) ? deviceId : null;

    public string? ResolveConnection(string deviceId) =>
        deviceToConnection.TryGetValue(deviceId, out var connectionId) ? connectionId : null;

    public bool IsConnected(string deviceId) =>
        deviceToConnection.ContainsKey(deviceId);

    // 切断された接続を解除し、最新の接続だった場合のみ端末 ID を返す(再接続後の遅延切断を無視するため)
    public string? Unregister(string connectionId)
    {
        if (!connectionToDevice.TryRemove(connectionId, out var deviceId))
        {
            return null;
        }

        if (deviceToConnection.TryGetValue(deviceId, out var current) && (current == connectionId))
        {
            deviceToConnection.TryRemove(deviceId, out _);
            return deviceId;
        }

        return null;
    }

    public IReadOnlyList<KeyValuePair<string, string>> Snapshot() =>
        [.. deviceToConnection];
}
