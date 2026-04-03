namespace DeviceManager.Services;

using System.Collections.Concurrent;

/// <summary>
/// In-process event bus for broadcasting application events to UI subscribers.
/// Replaces per-page SignalR client connections since Blazor Server already runs over SignalR.
/// </summary>
public sealed class AppEventBus
{
    private readonly ConcurrentDictionary<string, List<Func<object?, Task>>> handlers = new();

    public IDisposable Subscribe(string eventName, Func<object?, Task> handler)
    {
        var list = handlers.GetOrAdd(eventName, _ => []);
        lock (list)
        {
            list.Add(handler);
        }
        return new Subscription(this, eventName, handler);
    }

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

    private sealed class Subscription(AppEventBus bus, string eventName, Func<object?, Task> handler) : IDisposable
    {
        public void Dispose() => bus.Unsubscribe(eventName, handler);
    }
}

public static class AppEvents
{
    public const string DeviceConnected = nameof(DeviceConnected);
    public const string DeviceDisconnected = nameof(DeviceDisconnected);
    public const string DeviceStatusUpdated = nameof(DeviceStatusUpdated);
    public const string MessageReceived = nameof(MessageReceived);
    public const string LogReceived = nameof(LogReceived);
}
