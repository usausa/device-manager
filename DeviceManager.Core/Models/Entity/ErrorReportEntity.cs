namespace DeviceManager.Models.Entity;

public sealed class ErrorReportEntity
{
    public long ReportId { get; set; }

    public string DeviceId { get; set; } = default!;

    public string ExceptionType { get; set; } = default!;

    public string Message { get; set; } = default!;

    public string? StackTrace { get; set; }

    public string? InnerException { get; set; }

    public string? AppVersion { get; set; }

    public string? OsVersion { get; set; }

    public DateTime OccurredAt { get; set; }

    public DateTime ReceivedAt { get; set; }
}
