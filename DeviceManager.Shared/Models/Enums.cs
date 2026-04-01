namespace DeviceManager.Shared.Models;

public enum DeviceConnectionStatus
{
    Inactive = 0,
    Active = 1,
    Warning = 2,
    Error = 3
}

public enum MessageDirection
{
    ServerToDevice = 0,
    DeviceToServer = 1
}

public enum MessageStatus
{
    Sent = 0,
    Delivered = 1,
    Read = 2,
    Failed = 3
}

public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}
