namespace DeviceManager.Server.Web.Components.Dialogs;

public sealed partial class AppMessageBox
{
    [Parameter]
    public required MessageBoxType Type { get; set; }

    [Parameter]
    public required string Title { get; set; }

    [Parameter]
    public required string Message { get; set; }

    [CascadingParameter]
    public required MudDialogInstance MudDialog { get; set; }

    private void OnCloseClick() => MudDialog.Close();

    private void OnOkClick() => MudDialog.Close(true);

    private void OnCancelClick() => MudDialog.Close(false);

    private void HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            MudDialog.Close();
        }
    }
}
