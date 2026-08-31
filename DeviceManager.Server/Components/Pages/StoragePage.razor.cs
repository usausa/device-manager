namespace DeviceManager.Server.Components.Pages;

using DeviceManager.Infrastructure.Storage;
using DeviceManager.Server.Components.Dialogs;
using DeviceManager.Server.Infrastructure.Components;
using DeviceManager.Server.Infrastructure.IO;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using MudBlazor;

public sealed partial class StoragePage
{
    private const long MaxFileSize = 100L * 1024 * 1024;

    private List<StorageEntry> entries = [];

    private bool uploading;

    private int progress;

    [Inject]
    public required IStorage Storage { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Parameter]
    public string? Path { get; set; }

    private string CurrentPath => Path ?? string.Empty;

    protected override Task OnParametersSetAsync() =>
        ReloadAsync();

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    private async Task ReloadAsync()
    {
        if (!await Storage.DirectoryExistsAsync(CurrentPath))
        {
            entries = [];
            return;
        }

        var list = await Storage.ListEntriesAsync(CurrentPath);
        // ディレクトリ優先で名前順
        entries = [.. list.OrderByDescending(static x => x.Directory).ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)];
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    private static string EncodePath(string path) =>
        String.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    private string GetChildLink(string name) =>
        "storage/" + EncodePath(CombinePath(CurrentPath, name));

    private string GetDownloadLink(string name) =>
        "api/storage/" + EncodePath(CombinePath(CurrentPath, name));

    private static string CombinePath(string path, string name) =>
        path.Length == 0 ? name : $"{path}/{name}";

    // パンくず(最終要素はリンクなし)
    private List<(string Name, string? Link)> GetBreadcrumbs()
    {
        var list = new List<(string, string?)>();
        if (CurrentPath.Length == 0)
        {
            return list;
        }

        var segments = CurrentPath.Split('/');
        var current = string.Empty;
        for (var i = 0; i < segments.Length; i++)
        {
            current = CombinePath(current, segments[i]);
            list.Add((segments[i], i == segments.Length - 1 ? null : "storage/" + EncodePath(current)));
        }

        return list;
    }

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private async Task UploadAsync(IBrowserFile? file)
    {
        if (file is null)
        {
            return;
        }

        uploading = true;
        progress = 0;
        try
        {
            var fileName = System.IO.Path.GetFileName(file.Name);
            await using var browser = file.OpenReadStream(MaxFileSize);
            await using var progressStream = new ReadProgressStream(browser, file.Size, OnProgress);
            await Storage.WriteAsync(CombinePath(CurrentPath, fileName), progressStream);

            Snackbar.AddSuccess($"{fileName} をアップロードしました。");
        }
        catch (IOException)
        {
            Snackbar.AddError("アップロードに失敗しました。");
        }
        finally
        {
            uploading = false;
        }

        await ReloadAsync();
    }

    private void OnProgress(int percent)
    {
        _ = InvokeAsync(() =>
        {
            progress = percent;
            StateHasChanged();
        });
    }

    private async Task CreateFolderAsync()
    {
        var reference = await DialogService.ShowAsync<TextInputDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(TextInputDialog.Title), "フォルダ作成" },
                { nameof(TextInputDialog.Label), "フォルダ名" }
            });
        var result = await reference.Result;
        if (result is not { Canceled: false })
        {
            return;
        }

        var name = (string)result.Data!;
        try
        {
            await Storage.CreateDirectoryAsync(CombinePath(CurrentPath, name));
            Snackbar.AddSuccess("フォルダを作成しました。");
        }
        catch (Exception ex) when (ex is IOException or StorageException or ArgumentException)
        {
            Snackbar.AddError("フォルダの作成に失敗しました。");
        }

        await ReloadAsync();
    }

    private async Task DeleteAsync(StorageEntry entry)
    {
        var message = entry.Directory
            ? $"フォルダ {entry.Name} を削除してよろしいですか？中のファイルもすべて削除されます。"
            : $"{entry.Name} を削除してよろしいですか？";
        if (!await DialogService.ShowConfirm("削除", message))
        {
            return;
        }

        try
        {
            await Storage.DeleteAsync(CombinePath(CurrentPath, entry.Name));
            Snackbar.AddSuccess("削除しました。");
        }
        catch (IOException)
        {
            Snackbar.AddError("削除に失敗しました。");
        }

        await ReloadAsync();
    }
}
