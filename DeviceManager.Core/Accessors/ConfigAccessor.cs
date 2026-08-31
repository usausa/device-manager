namespace DeviceManager.Accessors;

[DataAccessor]
public sealed partial class ConfigAccessor
{
    [Execute]
    public partial void Create();

    // 共通コンフィグ

    [Query]
    public partial ValueTask<List<CommonConfigEntity>> QueryCommonAsync();

    [QueryFirst]
    public partial ValueTask<CommonConfigEntity?> QueryCommonEntryAsync(string key);

    [Execute]
    public partial ValueTask<int> UpsertCommonAsync(string key, string value, string? description, DateTime updatedAt);

    [Execute]
    public partial ValueTask<int> DeleteCommonAsync(string key);

    // 端末別コンフィグ

    [Query]
    public partial ValueTask<List<DeviceConfigEntity>> QueryDeviceAsync(string deviceId);

    [QueryFirst]
    public partial ValueTask<DeviceConfigEntity?> QueryDeviceEntryAsync(string deviceId, string key);

    [Execute]
    public partial ValueTask<int> UpsertDeviceAsync(string deviceId, string key, string value, string? description, DateTime updatedAt);

    [Execute]
    public partial ValueTask<int> DeleteDeviceEntryAsync(string deviceId, string key);

    [Execute]
    public partial ValueTask<int> DeleteDeviceAsync(string deviceId);

    // 変更履歴

    [Execute]
    public partial ValueTask<int> InsertHistoryAsync(string scope, string key, string? oldValue, string? newValue, DateTime changedAt);

    [Query]
    public partial ValueTask<List<ConfigHistoryEntity>> QueryHistoryAsync(int take);
}
