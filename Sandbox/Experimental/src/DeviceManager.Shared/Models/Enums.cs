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
