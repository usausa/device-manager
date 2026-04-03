namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;

public partial class DeviceDetailPage : IDisposable
{
    [Parameter] public string DeviceId { get; set; } = string.Empty;

    [Inject] public DeviceService DeviceService { get; set; } = default!;
    [Inject] public MessageService MessageService { get; set; } = default!;
    [Inject] public ConfigService ConfigService { get; set; } = default!;
    [Inject] public DataStoreService DataStoreService { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public AppEventBus EventBus { get; set; } = default!;

    private DeviceDetail? device;
    private List<DeviceStatusReport>? statusHistory;
    private List<ServerMessage>? messages;
    private List<ConfigEntry>? resolvedConfig;
    private List<DataStoreEntry>? dataEntries;

    private IDisposable? statusSub;

    protected override async Task OnInitializedAsync()
    {
        await LoadDeviceAsync();
        statusHistory = await DeviceService.GetStatusHistoryAsync(DeviceId);

        statusSub = EventBus.Subscribe(AppEvents.DeviceStatusUpdated, async payload =>
        {
            if (payload is string id && id == DeviceId)
            {
                await LoadDeviceAsync();
                statusHistory = await DeviceService.GetStatusHistoryAsync(DeviceId);
                await InvokeAsync(StateHasChanged);
            }
        });
    }

    private async Task LoadDeviceAsync()
    {
        device = await DeviceService.GetDeviceAsync(DeviceId);
    }

    private async Task OnTabChanged(int index)
    {
        switch (index)
        {
            case 0:
                statusHistory ??= await DeviceService.GetStatusHistoryAsync(DeviceId);
                break;
            case 1:
                messages ??= await MessageService.GetMessagesAsync(DeviceId, 0, 100);
                break;
            case 2:
                resolvedConfig ??= await ConfigService.GetResolvedConfigAsync(DeviceId);
                break;
            case 3:
                dataEntries ??= await DataStoreService.GetDeviceEntriesAsync(DeviceId);
                break;
        }
    }

    private void GoBack() => Navigation.NavigateTo("/");

    public void Dispose() => statusSub?.Dispose();
}
