namespace DeviceManager.Services;

using DeviceManager.Accessors;

public sealed class ConfigService
{
    private const string CommonScope = "common";

    private readonly ConfigAccessor configAccessor;

    private readonly TimeProvider timeProvider;

    public ConfigService(
        ConfigAccessor configAccessor,
        TimeProvider timeProvider)
    {
        this.configAccessor = configAccessor;
        this.timeProvider = timeProvider;
    }

    private static string DeviceScope(string deviceId) => $"device:{deviceId}";

    public void CreateTable() =>
        configAccessor.Create();

    //--------------------------------------------------------------------------------
    // Common
    //--------------------------------------------------------------------------------

    public ValueTask<List<CommonConfigEntity>> QueryCommonAsync() =>
        configAccessor.QueryCommonAsync();

    public async ValueTask SetCommonAsync(string key, string value, string? description)
    {
        var now = timeProvider.GetLocalNow().DateTime;
        var old = await configAccessor.QueryCommonEntryAsync(key);
        await configAccessor.UpsertCommonAsync(key, value, description, now);
        await configAccessor.InsertHistoryAsync(CommonScope, key, old?.Value, value, now);
    }

    public async ValueTask<bool> DeleteCommonAsync(string key)
    {
        var now = timeProvider.GetLocalNow().DateTime;
        var old = await configAccessor.QueryCommonEntryAsync(key);
        if (old is null)
        {
            return false;
        }

        await configAccessor.DeleteCommonAsync(key);
        await configAccessor.InsertHistoryAsync(CommonScope, key, old.Value, null, now);
        return true;
    }

    //--------------------------------------------------------------------------------
    // Device
    //--------------------------------------------------------------------------------

    public ValueTask<List<DeviceConfigEntity>> QueryDeviceAsync(string deviceId) =>
        configAccessor.QueryDeviceAsync(deviceId);

    public async ValueTask SetDeviceAsync(string deviceId, string key, string value, string? description)
    {
        var now = timeProvider.GetLocalNow().DateTime;
        var old = await configAccessor.QueryDeviceEntryAsync(deviceId, key);
        await configAccessor.UpsertDeviceAsync(deviceId, key, value, description, now);
        await configAccessor.InsertHistoryAsync(DeviceScope(deviceId), key, old?.Value, value, now);
    }

    public async ValueTask<bool> DeleteDeviceEntryAsync(string deviceId, string key)
    {
        var now = timeProvider.GetLocalNow().DateTime;
        var old = await configAccessor.QueryDeviceEntryAsync(deviceId, key);
        if (old is null)
        {
            return false;
        }

        await configAccessor.DeleteDeviceEntryAsync(deviceId, key);
        await configAccessor.InsertHistoryAsync(DeviceScope(deviceId), key, old.Value, null, now);
        return true;
    }

    public ValueTask<int> DeleteDeviceAsync(string deviceId) =>
        configAccessor.DeleteDeviceAsync(deviceId);

    //--------------------------------------------------------------------------------
    // Resolved
    //--------------------------------------------------------------------------------

    // 共通コンフィグに端末別コンフィグを上書きマージした解決済みの値を取得する
    public async ValueTask<List<ConfigValue>> QueryResolvedAsync(string deviceId)
    {
        var merged = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in await configAccessor.QueryCommonAsync())
        {
            merged[entry.Key] = entry.Value;
        }

        foreach (var entry in await configAccessor.QueryDeviceAsync(deviceId))
        {
            merged[entry.Key] = entry.Value;
        }

        return merged.Select(static x => new ConfigValue { Key = x.Key, Value = x.Value }).ToList();
    }

    //--------------------------------------------------------------------------------
    // History
    //--------------------------------------------------------------------------------

    public ValueTask<List<ConfigHistoryEntity>> QueryHistoryAsync(int take) =>
        configAccessor.QueryHistoryAsync(take);
}
