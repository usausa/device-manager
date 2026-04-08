namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;

public partial class CrashReports : IDisposable
{
    [Inject]
    public CrashReportService CrashReportService { get; set; } = default!;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    [Inject]
    public DeviceEventService EventBus { get; set; } = default!;

    private List<CrashReport> reports = [];
    private string filterDeviceId = string.Empty;
    private bool isLoading;
    private IDisposable? crashSub;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        crashSub = EventBus.Subscribe(AppEvents.CrashReportReceived, async _ =>
        {
            await LoadAsync();
            await InvokeAsync(StateHasChanged);
        });
    }

    internal async Task LoadAsync()
    {
        isLoading = true;
        var deviceId = string.IsNullOrWhiteSpace(filterDeviceId) ? null : filterDeviceId.Trim();
        reports = await CrashReportService.GetReportsAsync(deviceId, 0, 200);
        isLoading = false;
    }

    private async Task ShowDetailAsync(TableRowClickEventArgs<CrashReport> args)
    {
        if (args.Item is null)
        {
            return;
        }

        var parameters = new DialogParameters<CrashReportDetailDialog>
        {
            { x => x.Report, args.Item }
        };
        await DialogService.ShowAsync<CrashReportDetailDialog>(
            $"Crash Report #{args.Item.ReportId}", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true });
    }

    public void Dispose() => crashSub?.Dispose();
}
