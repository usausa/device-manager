namespace DeviceManager.Server.Infrastructure.Devices;

using DeviceManager.Server.Hubs;
using DeviceManager.Server.Infrastructure.Notifications;

using Microsoft.AspNetCore.SignalR;

// サーバーから端末への送信(メッセージ / コンフィグ再読込)
public sealed class DeviceMessenger
{
    private readonly IHubContext<DeviceHub> hubContext;

    private readonly DeviceConnectionRegistry registry;

    private readonly MessageService messageService;

    private readonly ConfigService configService;

    private readonly DeviceEventBus eventBus;

    public DeviceMessenger(
        IHubContext<DeviceHub> hubContext,
        DeviceConnectionRegistry registry,
        MessageService messageService,
        ConfigService configService,
        DeviceEventBus eventBus)
    {
        this.hubContext = hubContext;
        this.registry = registry;
        this.messageService = messageService;
        this.configService = configService;
        this.eventBus = eventBus;
    }

    // メッセージ送信(deviceId が null は全体送信)。履歴を保存し配送状態を返す
    public async ValueTask<MessageDeliveryStatus> SendMessageAsync(string? deviceId, string messageType, string content, CancellationToken cancellationToken = default)
    {
        var entity = await messageService.AddAsync(deviceId, MessageDirection.ServerToDevice, messageType, content, MessageDeliveryStatus.Sent);
        var message = new DeviceMessage
        {
            MessageId = entity.MessageId,
            DeviceId = deviceId,
            MessageType = messageType,
            Content = content,
            Timestamp = entity.CreatedAt
        };

        var status = MessageDeliveryStatus.Sent;
        if (deviceId is null)
        {
            await hubContext.Clients.All.SendAsync(DeviceHubConstants.Callbacks.ReceiveMessage, message, cancellationToken);
        }
        else
        {
            var connectionId = registry.ResolveConnection(deviceId);
            if (connectionId is null)
            {
                status = MessageDeliveryStatus.Failed;
            }
            else
            {
                await hubContext.Clients.Client(connectionId).SendAsync(DeviceHubConstants.Callbacks.ReceiveMessage, message, cancellationToken);
                status = MessageDeliveryStatus.Delivered;
            }

            await messageService.UpdateStatusAsync(entity.MessageId, status);
        }

        eventBus.Publish(DeviceEventType.MessageReceived, deviceId);
        return status;
    }

    // コンフィグ変更を接続中の端末へ配信する(deviceId が null は共通変更 = 全接続端末)
    public async ValueTask NotifyConfigChangedAsync(string? deviceId, CancellationToken cancellationToken = default)
    {
        if (deviceId is null)
        {
            foreach (var pair in registry.Snapshot())
            {
                var values = await configService.QueryResolvedAsync(pair.Key);
                await hubContext.Clients.Client(pair.Value).SendAsync(DeviceHubConstants.Callbacks.ConfigReload, values, cancellationToken);
            }
        }
        else
        {
            var connectionId = registry.ResolveConnection(deviceId);
            if (connectionId is not null)
            {
                var values = await configService.QueryResolvedAsync(deviceId);
                await hubContext.Clients.Client(connectionId).SendAsync(DeviceHubConstants.Callbacks.ConfigReload, values, cancellationToken);
            }
        }
    }
}
