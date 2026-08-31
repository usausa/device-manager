namespace DeviceManager.Server.Application;

public static class Policies
{
    public const string Administrator = nameof(Administrator);

    // 端末(API キー認証)
    public const string Device = nameof(Device);

    // 端末または管理ユーザー(API キー / Cookie のいずれか)
    public const string DeviceOrUser = nameof(DeviceOrUser);
}

public static class Roles
{
    public const string Administrator = nameof(Administrator);
}
