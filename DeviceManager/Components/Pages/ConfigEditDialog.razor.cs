namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;

public partial class ConfigEditDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    public ConfigService ConfigService { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Parameter]
    public bool IsNew { get; set; }

    [Parameter]
    public string ConfigKey { get; set; } = string.Empty;

    [Parameter]
    public string InitialValue { get; set; } = string.Empty;

    [Parameter]
    public string InitialValueType { get; set; } = "string";

    [Parameter]
    public string InitialDescription { get; set; } = string.Empty;

    private string key = string.Empty;
    private string value = string.Empty;
    private string valueType = "string";
    private string description = string.Empty;

    protected override void OnInitialized()
    {
        key = ConfigKey;
        value = InitialValue;
        valueType = InitialValueType;
        description = InitialDescription;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            Snackbar.Add("Key and Value are required.", Severity.Warning);
            return;
        }

        var entry = new ConfigEntry
        {
            Key = key.Trim(),
            Value = value,
            ValueType = valueType,
            Description = string.IsNullOrWhiteSpace(description) ? null : description
        };

        await ConfigService.SetCommonConfigAsync(key.Trim(), entry);
        Snackbar.Add(IsNew ? $"Config '{key}' created." : $"Config '{key}' updated.", Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
    }

    private void Cancel() => MudDialog.Cancel();
}
