namespace DeviceManager.Services;

using DeviceManager.Accessors;

public sealed class LogService
{
    private readonly DeviceLogAccessor deviceLogAccessor;

    public LogService(DeviceLogAccessor deviceLogAccessor)
    {
        this.deviceLogAccessor = deviceLogAccessor;
    }

    public void CreateTable() =>
        deviceLogAccessor.Create();

    public async ValueTask AddRangeAsync(string deviceId, IEnumerable<DeviceLogRecord> records)
    {
        foreach (var record in records)
        {
            await deviceLogAccessor.InsertAsync(deviceId, (int)record.Level, record.Category, record.Message, record.Exception, record.Timestamp);
        }
    }

    public ValueTask<List<DeviceLogEntity>> QueryLatestAsync(string? deviceId, DeviceLogLevel minLevel, int take) =>
        deviceLogAccessor.QueryLatestAsync(String.IsNullOrEmpty(deviceId) ? null : deviceId, (int)minLevel, take);

    public ValueTask<int> DeleteByDeviceAsync(string deviceId) =>
        deviceLogAccessor.DeleteByDeviceAsync(deviceId);

    public ValueTask<int> CleanupAsync(DateTime threshold) =>
        deviceLogAccessor.DeleteOlderAsync(threshold);
}
