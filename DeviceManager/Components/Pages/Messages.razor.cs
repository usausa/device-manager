namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;

public partial class Messages : IDisposable
{
    [Inject]
    public MessageService MessageService { get; set; } = default!;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public DeviceEventService EventBus { get; set; } = default!;

    private List<ServerMessage> messages = [];
    private string filterDeviceId = string.Empty;
    private bool isLoading;
    private IDisposable? messageSub;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        messageSub = EventBus.Subscribe(AppEvents.MessageReceived, async _ =>
        {
            await LoadAsync();
            await InvokeAsync(StateHasChanged);
        });
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        var deviceId = string.IsNullOrWhiteSpace(filterDeviceId) ? null : filterDeviceId.Trim();
        messages = await MessageService.GetMessagesAsync(deviceId, 0, 200);
        isLoading = false;
    }

    private async Task ShowSendDialogAsync()
    {
        var dialog = await DialogService.ShowAsync<SendMessageDialog>("Send Message");
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await LoadAsync();
        }
    }

    public void Dispose() => messageSub?.Dispose();
}
