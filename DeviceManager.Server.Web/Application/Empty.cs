namespace DeviceManager.Server.Web.Application;

public static class Empty<T>
    where T : new()
{
    public static T Instance { get; } = new();
}
