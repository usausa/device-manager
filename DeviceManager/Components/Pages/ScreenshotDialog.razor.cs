namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;

public partial class ScreenshotDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public ScreenshotResult Result { get; set; } = default!;

    private void Close() => MudDialog.Close();
}
