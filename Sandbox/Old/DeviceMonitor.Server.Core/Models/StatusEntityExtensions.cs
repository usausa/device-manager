namespace DeviceMonitor.Server.Models;

public static class StatusEntityExtensions
{
    public static bool IsRunning(this StatusEntity entity) => entity.Timestamp > DateTime.UtcNow.AddMinutes(-30);

    public static bool IsBatteryWarning(this StatusEntity entity) => entity.Battery <= 20;

    public static bool HasLocation(this StatusEntity entity) => entity.LastLocationAt is not null;
}
