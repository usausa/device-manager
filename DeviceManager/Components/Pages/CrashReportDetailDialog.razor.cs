namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;

public partial class CrashReportDetailDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public CrashReport Report { get; set; } = default!;

    private void Close() => MudDialog.Close();
}
