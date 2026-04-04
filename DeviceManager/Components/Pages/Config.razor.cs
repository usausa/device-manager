namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;

public partial class Config
{
    [Inject]
    public ConfigService ConfigService { get; set; } = default!;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    private List<ConfigEntry> entries = [];
    private string searchText = string.Empty;
    private bool isLoading;

    private IEnumerable<ConfigEntry> FilteredEntries => string.IsNullOrEmpty(searchText)
        ? entries
        : entries.Where(e => e.Key.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                          || e.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        isLoading = true;
        entries = await ConfigService.GetCommonConfigAsync();
        isLoading = false;
    }

    private async Task ShowAddDialogAsync()
    {
        var parameters = new DialogParameters<ConfigEditDialog> { { x => x.IsNew, true } };
        var dialog = await DialogService.ShowAsync<ConfigEditDialog>("Add Config", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await LoadAsync();
        }
    }

    private async Task ShowEditDialogAsync(ConfigEntry entry)
    {
        var parameters = new DialogParameters<ConfigEditDialog>
        {
            { x => x.IsNew, false },
            { x => x.ConfigKey, entry.Key },
            { x => x.InitialValue, entry.Value },
            { x => x.InitialValueType, entry.ValueType },
            { x => x.InitialDescription, entry.Description ?? string.Empty }
        };
        var dialog = await DialogService.ShowAsync<ConfigEditDialog>("Edit Config", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await LoadAsync();
        }
    }

    private async Task DeleteAsync(ConfigEntry entry)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete", $"Delete config '{entry.Key}'?", yesText: "Delete", cancelText: "Cancel");
        if (confirmed != true)
        {
            return;
        }

        await ConfigService.DeleteCommonConfigAsync(entry.Key);
        Snackbar.Add($"Config '{entry.Key}' deleted.", Severity.Success);
        await LoadAsync();
    }
}
