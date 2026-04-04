namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;

public partial class DataStore
{
    [Inject]
    public DataStoreService DataStoreService { get; set; } = default!;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    private List<DataStoreEntry> entries = [];
    private string searchText = string.Empty;
    private bool isLoading;

    private IEnumerable<DataStoreEntry> FilteredEntries => string.IsNullOrEmpty(searchText)
        ? entries
        : entries.Where(e => e.Key.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                          || e.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        isLoading = true;
        entries = await DataStoreService.GetCommonEntriesAsync();
        isLoading = false;
    }

    private async Task ShowAddDialogAsync()
    {
        var parameters = new DialogParameters<DataStoreEditDialog> { { x => x.IsNew, true } };
        var dialog = await DialogService.ShowAsync<DataStoreEditDialog>("Add Entry", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await LoadAsync();
        }
    }

    private async Task ShowEditDialogAsync(DataStoreEntry entry)
    {
        var parameters = new DialogParameters<DataStoreEditDialog>
        {
            { x => x.IsNew, false },
            { x => x.Key, entry.Key },
            { x => x.InitialValue, entry.Value }
        };
        var dialog = await DialogService.ShowAsync<DataStoreEditDialog>("Edit Entry", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await LoadAsync();
        }
    }

    private async Task DeleteAsync(DataStoreEntry entry)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete", $"Delete key '{entry.Key}'?", yesText: "Delete", cancelText: "Cancel");
        if (confirmed != true)
        {
            return;
        }

        await DataStoreService.DeleteCommonEntryAsync(entry.Key);
        Snackbar.Add($"Key '{entry.Key}' deleted.", Severity.Success);
        await LoadAsync();
    }
}
