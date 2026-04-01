namespace DeviceManager.Services;

using System.Collections.Concurrent;
using System.Threading.Channels;
using global::DeviceManager.Shared.Grpc;

public sealed class GrpcEventDispatcher
{
    private readonly ConcurrentDictionary<string, List<Channel<ServerEvent>>> subscribers = new();
    private readonly object syncLock = new();

    public void RegisterSubscriber(string deviceId, Channel<ServerEvent> channel)
    {
        lock (syncLock)
        {
            var list = subscribers.GetOrAdd(deviceId, _ => []);
            list.Add(channel);
        }
    }

    public void UnregisterSubscriber(string deviceId, Channel<ServerEvent> channel)
    {
        lock (syncLock)
        {
            if (subscribers.TryGetValue(deviceId, out var list))
            {
                list.Remove(channel);
                if (list.Count == 0)
                {
                    subscribers.TryRemove(deviceId, out _);
                }
            }
        }
    }

    public async Task DispatchEventAsync(string deviceId, ServerEvent serverEvent)
    {
        List<Channel<ServerEvent>>? channels;
        lock (syncLock)
        {
            if (!subscribers.TryGetValue(deviceId, out var list))
            {
                return;
            }

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
                // Subscriber disconnected
            }
        }
    }

    public async Task BroadcastEventAsync(ServerEvent serverEvent)
    {
        List<(string DeviceId, List<Channel<ServerEvent>> Channels)> snapshot;
        lock (syncLock)
        {
            snapshot = subscribers.Select(kvp => (kvp.Key, new List<Channel<ServerEvent>>(kvp.Value))).ToList();
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
