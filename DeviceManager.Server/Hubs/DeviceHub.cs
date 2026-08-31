namespace DeviceManager.Server.Hubs;

using DeviceManager.Server.Infrastructure.Authentication;
using DeviceManager.Server.Infrastructure.Devices;
using DeviceManager.Server.Infrastructure.Notifications;

using Microsoft.AspNetCore.SignalR;

// 端末通信ハブ(API キー認証)
[Authorize(AuthenticationSchemes = ApiKeyDefaults.AuthenticationScheme)]
public sealed class DeviceHub : Hub
{
    private readonly ILogger<DeviceHub> log;

    private readonly DeviceConnectionRegistry registry;

    private readonly DeviceService deviceService;

    private readonly MessageService messageService;

    private readonly ConfigService configService;

    private readonly DeviceEventBus eventBus;

    public DeviceHub(
        ILogger<DeviceHub> log,
        DeviceConnectionRegistry registry,
        DeviceService deviceService,
        MessageService messageService,
        ConfigService configService,
        DeviceEventBus eventBus)
    {
        this.log = log;
        this.registry = registry;
        this.deviceService = deviceService;
        this.messageService = messageService;
        this.configService = configService;
        this.eventBus = eventBus;
    }

    // 端末登録。接続と端末を対応付け、解決済みコンフィグを配信する
    public async Task Register(DeviceRegistration registration)
    {
        registry.Register(Context.ConnectionId, registration.DeviceId);
        await deviceService.RegisterAsync(registration);

        log.InfoDeviceConnected(registration.DeviceId);
        eventBus.Publish(DeviceEventType.Connected, registration.DeviceId);

        var values = await configService.QueryResolvedAsync(registration.DeviceId);
        await Clients.Caller.SendAsync(DeviceHubConstants.Callbacks.ConfigReload, values);
    }

    public async Task SendMessage(string messageType, string content)
    {
        var deviceId = registry.ResolveDevice(Context.ConnectionId);
        if (deviceId is null)
        {
            return;
        }

        await messageService.AddAsync(deviceId, MessageDirection.DeviceToServer, messageType, content, MessageDeliveryStatus.Delivered);
        eventBus.Publish(DeviceEventType.MessageReceived, deviceId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var deviceId = registry.Unregister(Context.ConnectionId);
        if (deviceId is not null)
        {
            await deviceService.DisconnectAsync(deviceId);

            log.InfoDeviceDisconnected(deviceId);
            eventBus.Publish(DeviceEventType.Disconnected, deviceId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
