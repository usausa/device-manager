namespace DeviceManager.Services;

using System.Collections.Concurrent;

/// <summary>
/// In-memory store for screenshots captured by devices.
/// Retains the most recent screenshot per device (plus lookup by requestId).
/// </summary>
public sealed class ScreenshotStore
{
    private readonly ConcurrentDictionary<string, ScreenshotResult> byRequestId = new();
    private readonly ConcurrentDictionary<string, string> latestByDevice = new();

    /// <summary>Saves a screenshot and records it as the latest for its device.</summary>
    public void Save(ScreenshotResult result)
    {
        byRequestId[result.RequestId] = result;
        latestByDevice[result.DeviceId] = result.RequestId;
    }

    /// <summary>Returns a screenshot by request ID, or <see langword="null"/> if not found.</summary>
    public ScreenshotResult? GetByRequestId(string requestId)
        => byRequestId.GetValueOrDefault(requestId);

    /// <summary>Returns the latest screenshot for a device, or <see langword="null"/> if none exists.</summary>
    public ScreenshotResult? GetLatestForDevice(string deviceId)
    {
        if (latestByDevice.TryGetValue(deviceId, out var requestId))
        {
            return byRequestId.GetValueOrDefault(requestId);
        }

        return null;
    }

    /// <summary>Removes a screenshot by request ID.</summary>
    public void Remove(string requestId)
    {
        if (byRequestId.TryRemove(requestId, out var result))
        {
            // Remove the "latest" pointer if it still points to this request
            latestByDevice.TryUpdate(result.DeviceId, string.Empty, requestId);
        }
    }
}
