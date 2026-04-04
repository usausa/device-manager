namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;

public partial class TextInputDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public string Label { get; set; } = "Value";

    private string value = string.Empty;

    private void Submit() => MudDialog.Close(DialogResult.Ok(value));
    private void Cancel() => MudDialog.Cancel();
}
