namespace DeviceManager.Server.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class TextInputDialog
{
    private string value = string.Empty;

    [Parameter]
    public required string Title { get; set; }

    [Parameter]
    public required string Label { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    private void OnOkClick() => MudDialog.Close(DialogResult.Ok(value.Trim()));

    private void OnCancelClick() => MudDialog.Cancel();
}
