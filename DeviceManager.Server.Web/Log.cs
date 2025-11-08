namespace DeviceManager.Server.Web;

internal static partial class Log
{
    // Request

    [LoggerMessage(Level = LogLevel.Error, Message = "Request failed.")]
    public static partial void ErrorRequestFailed(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid response.")]
    public static partial void WarnInvalidResponse(this ILogger logger);

    // Device

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connect failed.")]
    public static partial void WarnConnectFailed(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection timeout.")]
    public static partial void WarnConnectionTimeout(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Connection closed.")]
    public static partial void WarnConnectionClosed(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Device response error. resultCode=[{resultCode}]")]
    public static partial void WarnDeviceResponseError(this ILogger logger, int resultCode);
}
