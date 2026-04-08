namespace DeviceManager.Services;

/// <summary>
/// Named constants for in-process application events published by <see cref="DeviceEventService"/>.
/// </summary>
public static class AppEvents
{
    public const string DeviceConnected = nameof(DeviceConnected);
    public const string DeviceDisconnected = nameof(DeviceDisconnected);
    public const string DeviceStatusUpdated = nameof(DeviceStatusUpdated);
    public const string MessageReceived = nameof(MessageReceived);
    public const string LogReceived = nameof(LogReceived);
    public const string CrashReportReceived = nameof(CrashReportReceived);
}

