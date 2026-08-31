namespace DeviceManager.Server.Components.Dialogs;

using DeviceManager.Server.Models.Forms;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ConfigEditDialog
{
    private static readonly ConfigFormValidator Validator = new();

    private MudForm form = default!;

    [Parameter]
    public required string Title { get; set; }

    [Parameter]
    public required ConfigForm Form { get; set; }

    [Parameter]
    public bool IsNew { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    private async Task OnOkClick()
    {
        await form.ValidateAsync();
        if (form.IsValid)
        {
            MudDialog.Close(DialogResult.Ok(Form));
        }
    }

    private void OnCancelClick() => MudDialog.Cancel();
}
