namespace DeviceManager.Shared;

/// <summary>
/// SignalR hub path and method name constants shared between server and client.
/// </summary>
public static class HubConstants
{
    public const string DeviceHubPath = "/hubs/device";

    public static class ServerMethods
    {
        public const string ReceiveMessage = nameof(ReceiveMessage);
        public const string ReceiveCommand = nameof(ReceiveCommand);
        public const string ConfigUpdated = nameof(ConfigUpdated);
        public const string ConfigReload = nameof(ConfigReload);
    }

    public static class ClientMethods
    {
        public const string Register = nameof(Register);
        public const string ReportStatus = nameof(ReportStatus);
        public const string SendMessage = nameof(SendMessage);
        public const string SendLog = nameof(SendLog);
        public const string CommandResult = nameof(CommandResult);
    }

    public static class DashboardMethods
    {
        public const string DeviceConnected = nameof(DeviceConnected);
        public const string DeviceDisconnected = nameof(DeviceDisconnected);
        public const string DeviceStatusUpdated = nameof(DeviceStatusUpdated);
        public const string MessageReceived = nameof(MessageReceived);
        public const string LogReceived = nameof(LogReceived);
    }

    public static class Groups
    {
        public const string AllDevices = "all_devices";
        public const string Dashboard = "dashboard";
        public static string Device(string deviceId) => $"device_{deviceId}";
        public static string DeviceGroup(string groupName) => $"group_{groupName}";
    }
}
