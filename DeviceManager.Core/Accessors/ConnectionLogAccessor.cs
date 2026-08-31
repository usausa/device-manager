namespace DeviceManager.Accessors;

[DataAccessor]
public sealed partial class ConnectionLogAccessor
{
    [Execute]
    public partial void Create();

    [Execute]
    public partial ValueTask<int> InsertAsync(string deviceId, string eventType, DateTime createdAt);

    [Execute]
    public partial ValueTask<int> DeleteByDeviceAsync(string deviceId);

    [Execute]
    public partial ValueTask<int> DeleteOlderAsync(DateTime threshold);
}
