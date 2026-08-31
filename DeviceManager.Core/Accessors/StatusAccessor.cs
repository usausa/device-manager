namespace DeviceManager.Accessors;

[DataAccessor]
public sealed partial class StatusAccessor
{
    [Execute]
    public partial void Create();

    [Execute]
    public partial ValueTask<int> UpsertAsync(string deviceId, int level, double battery, int wifiRssi, string? apName, int moving, int scanCount, double progress1, double progress2, double? latitude, double? longitude, DateTime updatedAt);

    [Execute]
    public partial ValueTask<int> InsertHistoryAsync(string deviceId, int level, double battery, int wifiRssi, string? apName, int moving, int scanCount, double progress1, double progress2, double? latitude, double? longitude, DateTime createdAt);

    [QueryFirst]
    public partial ValueTask<DeviceStatusEntity?> QueryAsync(string deviceId);

    [ExecuteScalar]
    public partial ValueTask<int> CountActiveSinceAsync(DateTime since);

    [ExecuteScalar]
    public partial ValueTask<int> CountLowBatterySinceAsync(double threshold, DateTime since);

    [Execute]
    public partial ValueTask<int> DeleteAsync(string deviceId);

    [Execute]
    public partial ValueTask<int> DeleteHistoryByDeviceAsync(string deviceId);

    [Execute]
    public partial ValueTask<int> DeleteHistoryOlderAsync(DateTime threshold);
}
