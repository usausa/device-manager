using System.Collections.Concurrent;
using System.Threading.Channels;
using DeviceManager.Shared.Grpc;

namespace DeviceManager.Server.Web.Services;

public sealed class GrpcEventDispatcher
{
    private readonly ConcurrentDictionary<string, List<Channel<ServerEvent>>> _subscribers = new();
    private readonly object _lock = new();

    public void RegisterSubscriber(string deviceId, Channel<ServerEvent> channel)
    {
        lock (_lock)
        {
            var list = _subscribers.GetOrAdd(deviceId, _ => []);
            list.Add(channel);
        }
    }

    public void UnregisterSubscriber(string deviceId, Channel<ServerEvent> channel)
    {
        lock (_lock)
        {
            if (_subscribers.TryGetValue(deviceId, out var list))
            {
                list.Remove(channel);
                if (list.Count == 0)
                {
                    _subscribers.TryRemove(deviceId, out _);
                }
            }
        }
    }

    public async Task DispatchEventAsync(string deviceId, ServerEvent serverEvent)
    {
        List<Channel<ServerEvent>>? channels;
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(deviceId, out var list))
                return;
            channels = [.. list];
        }

        foreach (var channel in channels)
        {
            try
            {
                await channel.Writer.WriteAsync(serverEvent).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                // Subscriber disconnected; will be cleaned up by UnregisterSubscriber
            }
        }
    }

    public async Task BroadcastEventAsync(ServerEvent serverEvent)
    {
        List<(string DeviceId, List<Channel<ServerEvent>> Channels)> snapshot;
        lock (_lock)
        {
            snapshot = _subscribers.Select(kvp => (kvp.Key, new List<Channel<ServerEvent>>(kvp.Value))).ToList();
        }

        foreach (var (_, channels) in snapshot)
        {
            foreach (var channel in channels)
            {
                try
                {
                    await channel.Writer.WriteAsync(serverEvent).ConfigureAwait(false);
                }
                catch (ChannelClosedException)
                {
                    // Subscriber disconnected
                }
            }
        }
    }
}
