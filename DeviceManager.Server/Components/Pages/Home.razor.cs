namespace DeviceManager.Server.Components.Pages;

using DeviceManager.Server.Components.Dialogs;
using DeviceManager.Server.Infrastructure.Components;
using DeviceManager.Server.Infrastructure.Notifications;
using DeviceManager.Server.Models.Forms;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using MudBlazor;

public sealed partial class Home
{
    private DashboardSummary summary = new(0, 0, 0, 0, 0, 0);

    private List<DeviceListView> devices = [];

    private string? searchText;

    private DateTime now;

    [Inject]
    public required DeviceService DeviceService { get; set; }

    [Inject]
    public required DeviceUsecase DeviceUsecase { get; set; }

    [Inject]
    public required DeviceSetting Setting { get; set; }

    [Inject]
    public required DeviceEventBus EventBus { get; set; }

    [Inject]
    public required TimeProvider TimeProvider { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // 端末イベントを購読してリアルタイム更新(破棄時に解除)
        EventBus.Received += OnDeviceEvent;

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
        _ = InvokeAsync(async () =>
        {
            await ReloadAsync();
            StateHasChanged();
        });
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    private async Task ReloadAsync()
    {
        now = TimeProvider.GetLocalNow().DateTime;
        summary = await DeviceUsecase.QuerySummaryAsync(Setting.LowBatteryThreshold);
        devices = await DeviceService.QueryListAsync(searchText);
    }

    private Task SearchAsync() => ReloadAsync();

    private Task OnSearchKeyDown(KeyboardEventArgs args) =>
        args.Key == "Enter" ? SearchAsync() : Task.CompletedTask;

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private async Task AddAsync()
    {
        var form = await ShowEditDialog("端末追加", new DeviceForm(), isNew: true);
        if (form is null)
        {
            return;
        }

        if (await DeviceService.AddAsync(form.DeviceId, form.Name, form.GroupName, form.Note))
        {
            Snackbar.AddSuccess("追加しました。");
        }
        else
        {
            Snackbar.AddError("端末IDが重複しています。");
        }

        await ReloadAsync();
    }

    private async Task EditAsync(DeviceListView device)
    {
        var form = new DeviceForm
        {
            DeviceId = device.DeviceId,
            Name = device.Name,
            GroupName = device.GroupName,
            Note = device.Note,
            IsEnabled = device.IsEnabled != 0
        };
        var result = await ShowEditDialog("端末編集", form, isNew: false);
        if (result is null)
        {
            return;
        }

        if (await DeviceService.UpdateProfileAsync(result.DeviceId, result.Name, result.GroupName, result.Note, result.IsEnabled))
        {
            Snackbar.AddSuccess("更新しました。");
        }
        else
        {
            Snackbar.AddError("対象が存在しません。");
        }

        await ReloadAsync();
    }

    private async Task DeleteAsync(DeviceListView device)
    {
        if (!await DialogService.ShowConfirm("端末削除", $"{device.Name} ({device.DeviceId}) を削除してよろしいですか？関連するログ等も削除されます。"))
        {
            return;
        }

        if (await DeviceUsecase.DeleteDeviceAsync(device.DeviceId))
        {
            Snackbar.AddSuccess("削除しました。");
        }
        else
        {
            Snackbar.AddError("対象が存在しません。");
        }

        await ReloadAsync();
    }

    private async Task ShowLogsAsync(DeviceListView device)
    {
        var reference = await DialogService.ShowAsync<DeviceLogDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(DeviceLogDialog.DeviceId), device.DeviceId },
                { nameof(DeviceLogDialog.DeviceName), device.Name }
            },
            new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true });
        await reference.Result;
    }

    private async Task<DeviceForm?> ShowEditDialog(string title, DeviceForm form, bool isNew)
    {
        var reference = await DialogService.ShowAsync<DeviceEditDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(DeviceEditDialog.Title), title },
                { nameof(DeviceEditDialog.Form), form },
                { nameof(DeviceEditDialog.IsNew), isNew }
            });
        var result = await reference.Result;
        return (result is { Canceled: false }) ? (DeviceForm)result.Data! : null;
    }
}
