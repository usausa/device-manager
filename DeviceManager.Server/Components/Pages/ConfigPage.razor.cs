namespace DeviceManager.Server.Components.Pages;

using DeviceManager.Server.Components.Dialogs;
using DeviceManager.Server.Infrastructure.Components;
using DeviceManager.Server.Infrastructure.Devices;
using DeviceManager.Server.Models.Forms;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ConfigPage
{
    private const int HistoryRows = 50;

    private List<DeviceListView> devices = [];

    private List<CommonConfigEntity> commonEntries = [];

    private List<DeviceConfigEntity> deviceEntries = [];

    private List<ConfigHistoryEntity> histories = [];

    private string? deviceId;

    [Inject]
    public required DeviceService DeviceService { get; set; }

    [Inject]
    public required ConfigService ConfigService { get; set; }

    [Inject]
    public required DeviceMessenger Messenger { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    protected override async Task OnInitializedAsync()
    {
        devices = await DeviceService.QueryListAsync(null);
        await ReloadCommonAsync();
        await ReloadHistoryAsync();
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    private async Task ReloadCommonAsync()
    {
        commonEntries = await ConfigService.QueryCommonAsync();
    }

    private async Task ReloadDeviceAsync()
    {
        deviceEntries = deviceId is null ? [] : await ConfigService.QueryDeviceAsync(deviceId);
    }

    private async Task ReloadHistoryAsync()
    {
        histories = await ConfigService.QueryHistoryAsync(HistoryRows);
    }

    //--------------------------------------------------------------------------------
    // Common
    //--------------------------------------------------------------------------------

    private async Task AddCommonAsync()
    {
        var form = await ShowEditDialog("共通設定追加", new ConfigForm(), isNew: true);
        if (form is null)
        {
            return;
        }

        await ConfigService.SetCommonAsync(form.Key, form.Value, form.Description);
        await Messenger.NotifyConfigChangedAsync(null);
        Snackbar.AddSuccess("保存しました。");
        await ReloadCommonAsync();
        await ReloadHistoryAsync();
    }

    private async Task EditCommonAsync(CommonConfigEntity entity)
    {
        var form = await ShowEditDialog("共通設定編集", new ConfigForm { Key = entity.Key, Value = entity.Value, Description = entity.Description }, isNew: false);
        if (form is null)
        {
            return;
        }

        await ConfigService.SetCommonAsync(form.Key, form.Value, form.Description);
        await Messenger.NotifyConfigChangedAsync(null);
        Snackbar.AddSuccess("保存しました。");
        await ReloadCommonAsync();
        await ReloadHistoryAsync();
    }

    private async Task DeleteCommonAsync(CommonConfigEntity entity)
    {
        if (!await DialogService.ShowConfirm("共通設定削除", $"{entity.Key} を削除してよろしいですか？"))
        {
            return;
        }

        if (await ConfigService.DeleteCommonAsync(entity.Key))
        {
            await Messenger.NotifyConfigChangedAsync(null);
            Snackbar.AddSuccess("削除しました。");
        }
        else
        {
            Snackbar.AddError("対象が存在しません。");
        }

        await ReloadCommonAsync();
        await ReloadHistoryAsync();
    }

    //--------------------------------------------------------------------------------
    // Device
    //--------------------------------------------------------------------------------

    private async Task AddDeviceAsync()
    {
        if (deviceId is null)
        {
            return;
        }

        var form = await ShowEditDialog("端末別設定追加", new ConfigForm(), isNew: true);
        if (form is null)
        {
            return;
        }

        await ConfigService.SetDeviceAsync(deviceId, form.Key, form.Value, form.Description);
        await Messenger.NotifyConfigChangedAsync(deviceId);
        Snackbar.AddSuccess("保存しました。");
        await ReloadDeviceAsync();
        await ReloadHistoryAsync();
    }

    private async Task EditDeviceAsync(DeviceConfigEntity entity)
    {
        var form = await ShowEditDialog("端末別設定編集", new ConfigForm { Key = entity.Key, Value = entity.Value, Description = entity.Description }, isNew: false);
        if (form is null)
        {
            return;
        }

        await ConfigService.SetDeviceAsync(entity.DeviceId, form.Key, form.Value, form.Description);
        await Messenger.NotifyConfigChangedAsync(entity.DeviceId);
        Snackbar.AddSuccess("保存しました。");
        await ReloadDeviceAsync();
        await ReloadHistoryAsync();
    }

    private async Task DeleteDeviceAsync(DeviceConfigEntity entity)
    {
        if (!await DialogService.ShowConfirm("端末別設定削除", $"{entity.Key} を削除してよろしいですか？"))
        {
            return;
        }

        if (await ConfigService.DeleteDeviceEntryAsync(entity.DeviceId, entity.Key))
        {
            await Messenger.NotifyConfigChangedAsync(entity.DeviceId);
            Snackbar.AddSuccess("削除しました。");
        }
        else
        {
            Snackbar.AddError("対象が存在しません。");
        }

        await ReloadDeviceAsync();
        await ReloadHistoryAsync();
    }

    //--------------------------------------------------------------------------------
    // Dialog
    //--------------------------------------------------------------------------------

    private async Task<ConfigForm?> ShowEditDialog(string title, ConfigForm form, bool isNew)
    {
        var reference = await DialogService.ShowAsync<ConfigEditDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(ConfigEditDialog.Title), title },
                { nameof(ConfigEditDialog.Form), form },
                { nameof(ConfigEditDialog.IsNew), isNew }
            });
        var result = await reference.Result;
        return (result is { Canceled: false }) ? (ConfigForm)result.Data! : null;
    }
}
