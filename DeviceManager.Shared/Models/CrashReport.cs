namespace DeviceManager.Shared.Models;

/// <summary>
/// A crash report sent from a device to the server.
/// </summary>
public sealed class CrashReport
{
    public long ReportId { get; init; }
    public string DeviceId { get; init; } = string.Empty;
    public required string ExceptionType { get; init; }
    public required string Message { get; init; }
    public string? StackTrace { get; init; }
    public string? InnerException { get; init; }
    public string? AppVersion { get; init; }
    public string? OsVersion { get; init; }
    public IDictionary<string, string>? AdditionalData { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public DateTime ReceivedAt { get; init; }
}
