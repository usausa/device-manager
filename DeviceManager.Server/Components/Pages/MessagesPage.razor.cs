namespace DeviceManager.Server.Components.Pages;

using DeviceManager.Server.Components.Dialogs;
using DeviceManager.Server.Infrastructure.Components;
using DeviceManager.Server.Infrastructure.Devices;
using DeviceManager.Server.Infrastructure.Notifications;
using DeviceManager.Server.Models.Forms;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class MessagesPage
{
    // コンポーネント参照(破棄はレンダラーが管理)
    private MudDataGrid<MessageEntity> Grid { get; set; } = default!;

    private List<DeviceListView> devices = [];

    private string? deviceId;

    [Inject]
    public required DeviceService DeviceService { get; set; }

    [Inject]
    public required MessageUsecase MessageUsecase { get; set; }

    [Inject]
    public required DeviceMessenger Messenger { get; set; }

    [Inject]
    public required DeviceEventBus EventBus { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // メッセージイベントを購読してリアルタイム更新(破棄時に解除)
        EventBus.Received += OnDeviceEvent;

        devices = await DeviceService.QueryListAsync(null);
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
        if (e.Type != DeviceEventType.MessageReceived)
        {
            return;
        }

        _ = InvokeAsync(Grid.ReloadServerData);
    }

    //--------------------------------------------------------------------------------
    // Grid
    //--------------------------------------------------------------------------------

    private async Task<GridData<MessageEntity>> LoadServerData(GridState<MessageEntity> state, CancellationToken cancellationToken)
    {
        var result = await MessageUsecase.QueryPageAsync(deviceId, state.Page, state.PageSize);
        return new GridData<MessageEntity>
        {
            TotalItems = result.Total,
            Items = result.Items
        };
    }

    private Task ReloadAsync() => Grid.ReloadServerData();

    private Task OnFilterChanged() => Grid.ReloadServerData();

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private async Task SendAsync()
    {
        var reference = await DialogService.ShowAsync<SendMessageDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(SendMessageDialog.Form), new MessageForm { DeviceId = deviceId } },
                { nameof(SendMessageDialog.Devices), devices }
            });
        var result = await reference.Result;
        if (result is not { Canceled: false })
        {
            return;
        }

        var form = (MessageForm)result.Data!;
        var status = await Messenger.SendMessageAsync(
            String.IsNullOrEmpty(form.DeviceId) ? null : form.DeviceId,
            form.MessageType,
            form.Content);
        if (status == MessageDeliveryStatus.Failed)
        {
            Snackbar.AddWarning("端末が未接続のため配送できませんでした。");
        }
        else
        {
            Snackbar.AddSuccess("送信しました。");
        }

        await Grid.ReloadServerData();
    }
}
