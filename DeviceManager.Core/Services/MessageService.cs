namespace DeviceManager.Services;

using DeviceManager.Accessors;

public sealed class MessageService
{
    private readonly MessageAccessor messageAccessor;

    private readonly TimeProvider timeProvider;

    public MessageService(
        MessageAccessor messageAccessor,
        TimeProvider timeProvider)
    {
        this.messageAccessor = messageAccessor;
        this.timeProvider = timeProvider;
    }

    public void CreateTable() =>
        messageAccessor.Create();

    public async ValueTask<MessageEntity> AddAsync(string? deviceId, MessageDirection direction, string messageType, string content, MessageDeliveryStatus status)
    {
        var now = timeProvider.GetLocalNow().DateTime;
        var id = await messageAccessor.InsertAsync(deviceId, (int)direction, messageType, content, (int)status, now);
        return new MessageEntity
        {
            MessageId = id,
            DeviceId = deviceId,
            Direction = (int)direction,
            MessageType = messageType,
            Content = content,
            Status = (int)status,
            CreatedAt = now
        };
    }

    public ValueTask<int> UpdateStatusAsync(long messageId, MessageDeliveryStatus status) =>
        messageAccessor.UpdateStatusAsync(messageId, (int)status);

    public ValueTask<int> CountAsync(string? deviceId) =>
        messageAccessor.CountAsync(String.IsNullOrEmpty(deviceId) ? null : deviceId);

    public ValueTask<List<MessageEntity>> QueryPageAsync(string? deviceId, int offset, int size) =>
        messageAccessor.QueryPageAsync(String.IsNullOrEmpty(deviceId) ? null : deviceId, offset, size);

    public ValueTask<int> DeleteByDeviceAsync(string deviceId) =>
        messageAccessor.DeleteByDeviceAsync(deviceId);
}
