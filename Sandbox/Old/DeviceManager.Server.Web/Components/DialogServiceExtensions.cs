namespace DeviceManager.Server.Web.Components;

using DummyServer.Components.Dialogs;
using DummyServer.Models;

using MudBlazor;

public static class DialogServiceExtensions
{
    public static async ValueTask ShowInformation(this IDialogService dialog, string title, string message)
    {
        var reference = await dialog.ShowAsync<AppMessageBox>(
            string.Empty,
            new DialogParameters
            {
                { nameof(AppMessageBox.Type), MessageBoxType.Information },
                { nameof(AppMessageBox.Title), title },
                { nameof(AppMessageBox.Message), message }
            },
            null);
        await reference.Result;
    }

    public static async ValueTask<bool> ShowConfirm(this IDialogService dialog, string title, string message)
    {
        var reference = await dialog.ShowAsync<AppMessageBox>(
            string.Empty,
            new DialogParameters
            {
                { nameof(AppMessageBox.Type), MessageBoxType.Confirm },
                { nameof(AppMessageBox.Title), title },
                { nameof(AppMessageBox.Message), message }
            },
            null);
        var result = await reference.Result;
        return (bool?)result?.Data == true;
    }

    public static async ValueTask ShowDetail(this IDialogService dialog, MonitorEntity entity)
    {
        var reference = await dialog.ShowAsync<DetailDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(DetailDialog.Entity), entity }
            },
            new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                FullWidth = false,
                CloseOnEscapeKey = true
            });
        await reference.Result;
    }
}
