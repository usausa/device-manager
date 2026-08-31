namespace DeviceManager.Server.Components.Pages;

using DeviceManager.Server.Infrastructure.Notifications;

using Microsoft.AspNetCore.Components;

public sealed partial class LogsPage
{
    private const int MaxRows = 200;

    private List<DeviceListView> devices = [];

    private List<DeviceLogEntity> logs = [];

    private string? deviceId;

    private int minLevel;

    [Inject]
    public required DeviceService DeviceService { get; set; }

    [Inject]
    public required LogService LogService { get; set; }

    [Inject]
    public required DeviceEventBus EventBus { get; set; }

    [SupplyParameterFromQuery(Name = "deviceId")]
    public string? QueryDeviceId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        deviceId = QueryDeviceId;

        // ログ受信イベントを購読してリアルタイム更新(破棄時に解除)
        EventBus.Received += OnDeviceEvent;

        devices = await DeviceService.QueryListAsync(null);
        await ReloadAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            EventBus.Received -= OnDeviceEvent;
        }

        base.Dispose(disposing);
    }

    private void OnDeviceEvent(object? sender, DeviceEventArgs e)
    {
        if (e.Type != DeviceEventType.LogReceived)
        {
            return;
        }

        _ = InvokeAsync(async () =>
        {
            await ReloadAsync();
            StateHasChanged();
        });
    }

    private async Task ReloadAsync()
    {
        logs = await LogService.QueryLatestAsync(deviceId, (DeviceLogLevel)minLevel, MaxRows);
    }

    private Task OnFilterChanged() => ReloadAsync();
}
