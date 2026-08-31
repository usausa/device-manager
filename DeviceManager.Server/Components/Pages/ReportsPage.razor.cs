namespace DeviceManager.Server.Components.Pages;

using DeviceManager.Server.Components.Dialogs;
using DeviceManager.Server.Infrastructure.Components;
using DeviceManager.Server.Infrastructure.Notifications;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ReportsPage
{
    // コンポーネント参照(破棄はレンダラーが管理)
    private MudDataGrid<ErrorReportEntity> Grid { get; set; } = default!;

    private List<DeviceListView> devices = [];

    private string? deviceId;

    [Inject]
    public required DeviceService DeviceService { get; set; }

    [Inject]
    public required ErrorReportService ErrorReportService { get; set; }

    [Inject]
    public required ErrorReportUsecase ErrorReportUsecase { get; set; }

    [Inject]
    public required DeviceEventBus EventBus { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // エラーレポート受信イベントを購読してリアルタイム更新(破棄時に解除)
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
        if (e.Type != DeviceEventType.ErrorReportReceived)
        {
            return;
        }

        _ = InvokeAsync(Grid.ReloadServerData);
    }

    //--------------------------------------------------------------------------------
    // Grid
    //--------------------------------------------------------------------------------

    private async Task<GridData<ErrorReportEntity>> LoadServerData(GridState<ErrorReportEntity> state, CancellationToken cancellationToken)
    {
        var result = await ErrorReportUsecase.QueryPageAsync(deviceId, state.Page, state.PageSize);
        return new GridData<ErrorReportEntity>
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

    private Task OnRowClick(DataGridRowClickEventArgs<ErrorReportEntity> args) =>
        ShowDetailAsync(args.Item);

    private async Task ShowDetailAsync(ErrorReportEntity report)
    {
        var reference = await DialogService.ShowAsync<ReportDetailDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(ReportDetailDialog.Report), report }
            },
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
        await reference.Result;
    }

    private async Task DeleteAsync(ErrorReportEntity report)
    {
        if (!await DialogService.ShowConfirm("レポート削除", $"レポート {report.ReportId} を削除してよろしいですか？"))
        {
            return;
        }

        if (await ErrorReportService.DeleteAsync(report.ReportId))
        {
            Snackbar.AddSuccess("削除しました。");
        }
        else
        {
            Snackbar.AddError("対象が存在しません。");
        }

        await Grid.ReloadServerData();
    }
}
