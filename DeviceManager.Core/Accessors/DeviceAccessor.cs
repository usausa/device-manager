namespace DeviceManager.Accessors;

[DataAccessor]
public sealed partial class DeviceAccessor
{
    [Execute]
    public partial void Create();

    [Query]
    public partial ValueTask<List<DeviceListView>> QueryListAsync(string? filter);

    [QueryFirst]
    public partial ValueTask<DeviceEntity?> QueryAsync(string deviceId);

    [ExecuteScalar]
    public partial ValueTask<int> CountAsync();

    [Execute]
    public partial ValueTask<int> InsertAsync(string deviceId, string name, string? platform, int status, DateTime registeredAt, DateTime? lastConnectedAt);

    [Execute]
    public partial ValueTask<int> UpdateRegisterAsync(string deviceId, string name, string? platform, int status, DateTime lastConnectedAt);

    [Execute]
    public partial ValueTask<int> UpdateStateAsync(string deviceId, int status);

    [Execute]
    public partial ValueTask<int> UpdateConnectedAsync(string deviceId, int status, DateTime lastConnectedAt);

    [ExecuteScalar]
    public partial ValueTask<int> CountByStateAsync(int status);

    [Execute]
    public partial ValueTask<int> UpdateProfileAsync(string deviceId, string name, string? groupName, string? note, int isEnabled);

    [Execute]
    public partial ValueTask<int> DeleteAsync(string deviceId);
}
