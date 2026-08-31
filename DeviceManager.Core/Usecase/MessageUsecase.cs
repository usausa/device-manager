namespace DeviceManager.Usecase;

using DeviceManager.Services;

public sealed class MessageUsecase
{
    private readonly MessageService messageService;

    public MessageUsecase(MessageService messageService)
    {
        this.messageService = messageService;
    }

    public async ValueTask<PagedResult<MessageEntity>> QueryPageAsync(string? deviceId, int page, int size)
    {
        var total = await messageService.CountAsync(deviceId);
        var items = await messageService.QueryPageAsync(deviceId, page * size, size);
        return new PagedResult<MessageEntity>(total, page, size, items);
    }
}
