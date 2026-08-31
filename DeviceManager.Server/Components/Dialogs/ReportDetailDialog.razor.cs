namespace DeviceManager.Server.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ReportDetailDialog
{
    [Parameter]
    public required ErrorReportEntity Report { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    private void OnCloseClick() => MudDialog.Cancel();
}
