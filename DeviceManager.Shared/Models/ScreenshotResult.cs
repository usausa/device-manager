namespace DeviceManager.Shared.Models;

/// <summary>A screenshot captured by a device in response to a server request.</summary>
public sealed class ScreenshotResult
{
    public required string RequestId { get; init; }
    public required string DeviceId { get; init; }
    public required string Base64Data { get; init; }
    public string ContentType { get; init; } = "image/png";
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;
}
