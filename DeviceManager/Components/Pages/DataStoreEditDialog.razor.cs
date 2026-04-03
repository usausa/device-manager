namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;

public partial class DataStoreEditDialog
{
    [CascadingParameter] public IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] public DataStoreService DataStoreService { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public bool IsNew { get; set; }
    [Parameter] public string Key { get; set; } = string.Empty;
    [Parameter] public string InitialValue { get; set; } = string.Empty;

    private string key = string.Empty;
    private string value = string.Empty;

    protected override void OnInitialized()
    {
        key = Key;
        value = InitialValue;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            Snackbar.Add("Key and Value are required.", Severity.Warning);
            return;
        }

        await DataStoreService.SetCommonEntryAsync(key.Trim(), value);
        Snackbar.Add(IsNew ? $"Entry '{key}' created." : $"Entry '{key}' updated.", Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();
}
