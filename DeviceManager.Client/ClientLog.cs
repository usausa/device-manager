namespace DeviceManager.Client;

internal static partial class ClientLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Device manager connected. deviceId=[{deviceId}]")]
    public static partial void InfoConnected(this ILogger logger, string deviceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Device manager disconnected.")]
    public static partial void InfoDisconnected(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Status report failed.")]
    public static partial void WarnStatusReportFailed(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Telemetry store operation failed.")]
    public static partial void WarnTelemetryStoreFailed(this ILogger logger, Exception ex);
}
