namespace DeviceManager.Shared.Models;

/// <summary>
/// Represents a log entry sent from a device to the server.
/// </summary>
public sealed class LogEntry
{
    public long LogId { get; init; }
    public required string DeviceId { get; init; }
    public LogLevel Level { get; init; }
    public required string Category { get; init; }
    public required string Message { get; init; }
    public string? Exception { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
