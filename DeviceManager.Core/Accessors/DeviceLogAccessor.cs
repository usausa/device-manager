namespace DeviceManager.Accessors;

[DataAccessor]
public sealed partial class DeviceLogAccessor
{
    [Execute]
    public partial void Create();

    [Execute]
    public partial ValueTask<int> InsertAsync(string deviceId, int level, string category, string message, string? exception, DateTime createdAt);

    [Query]
    public partial ValueTask<List<DeviceLogEntity>> QueryLatestAsync(string? deviceId, int minLevel, int take);

    [Execute]
    public partial ValueTask<int> DeleteByDeviceAsync(string deviceId);

    [Execute]
    public partial ValueTask<int> DeleteOlderAsync(DateTime threshold);
}
