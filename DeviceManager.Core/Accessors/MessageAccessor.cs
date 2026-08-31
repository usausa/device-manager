namespace DeviceManager.Accessors;

[DataAccessor]
public sealed partial class MessageAccessor
{
    [Execute]
    public partial void Create();

    [ExecuteScalar]
    public partial ValueTask<long> InsertAsync(string? deviceId, int direction, string messageType, string content, int status, DateTime createdAt);

    [Execute]
    public partial ValueTask<int> UpdateStatusAsync(long messageId, int status);

    [ExecuteScalar]
    public partial ValueTask<int> CountAsync(string? deviceId);

    [Query]
    public partial ValueTask<List<MessageEntity>> QueryPageAsync(string? deviceId, int offset, int size);

    [Execute]
    public partial ValueTask<int> DeleteByDeviceAsync(string deviceId);
}
