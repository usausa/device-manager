namespace DeviceManager.Server.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class DeviceLogDialog
{
    private const int MaxRows = 50;

    private List<DeviceLogEntity> logs = [];

    [Inject]
    public required LogService LogService { get; set; }

    [Parameter]
    public required string DeviceId { get; set; }

    [Parameter]
    public required string DeviceName { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    protected override async Task OnInitializedAsync()
    {
        logs = await LogService.QueryLatestAsync(DeviceId, DeviceLogLevel.Trace, MaxRows);
    }

    private void OnCloseClick() => MudDialog.Cancel();
}
