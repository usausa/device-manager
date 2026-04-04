namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

public partial class Storage
{
    [Parameter] public string? SubPath { get; set; }

    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public IConfiguration Configuration { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;

    private string rootPath = string.Empty;
    private readonly List<StorageEntry> entries = [];
    private bool isLoading;
    private List<BreadcrumbItem> breadcrumbs = [];

    protected override void OnInitialized()
    {
        var configuredPath = Configuration["Storage:RootPath"];
        rootPath = !string.IsNullOrEmpty(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.Combine(AppContext.BaseDirectory, "storage");
        Directory.CreateDirectory(rootPath);
    }

    protected override async Task OnParametersSetAsync()
    {
        await LoadEntriesAsync();
        BuildBreadcrumbs();
    }

    private Task LoadEntriesAsync()
    {
        isLoading = true;
        entries.Clear();

        var fullPath = ResolveSafePath(SubPath);
        if (fullPath is null || !Directory.Exists(fullPath))
        {
            isLoading = false;
            return Task.CompletedTask;
        }

        // Parent nav
        if (!string.IsNullOrEmpty(SubPath))
        {
            entries.Add(new StorageEntry { Name = "..", IsDirectory = true, IsParent = true });
        }

        foreach (var dir in Directory.EnumerateDirectories(fullPath).OrderBy(d => d))
        {
            var info = new DirectoryInfo(dir);
            entries.Add(new StorageEntry
            {
                Name = info.Name,
                IsDirectory = true,
                LastModified = info.LastWriteTimeUtc
            });
        }

        foreach (var file in Directory.EnumerateFiles(fullPath).OrderBy(f => f))
        {
            var info = new FileInfo(file);
            entries.Add(new StorageEntry
            {
                Name = info.Name,
                IsDirectory = false,
                Length = info.Length,
                LastModified = info.LastWriteTimeUtc
            });
        }

        isLoading = false;
        return Task.CompletedTask;
    }

    private void BuildBreadcrumbs()
    {
        breadcrumbs = [new BreadcrumbItem("Storage", "/storage", icon: Icons.Material.Filled.Folder)];
        if (string.IsNullOrEmpty(SubPath)) return;
        var parts = SubPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var accumulated = string.Empty;
        for (var i = 0; i < parts.Length; i++)
        {
            accumulated = string.IsNullOrEmpty(accumulated) ? parts[i] : $"{accumulated}/{parts[i]}";
            var isLast = i == parts.Length - 1;
            breadcrumbs.Add(new BreadcrumbItem(parts[i], isLast ? null : $"/storage/{accumulated}", disabled: isLast));
        }
    }

    private Task OnRowClickAsync(StorageEntry entry)
    {
        if (!entry.IsDirectory) return Task.CompletedTask;

        if (entry.IsParent)
        {
            var parent = string.IsNullOrEmpty(SubPath) ? null : Path.GetDirectoryName(SubPath)?.Replace('\\', '/');
            Navigation.NavigateTo(string.IsNullOrEmpty(parent) ? "/storage" : $"/storage/{parent}");
        }
        else
        {
            var newPath = string.IsNullOrEmpty(SubPath) ? entry.Name : $"{SubPath}/{entry.Name}";
            Navigation.NavigateTo($"/storage/{newPath}");
        }

        return Task.CompletedTask;
    }

    private async Task UploadFilesAsync(IReadOnlyList<IBrowserFile> files)
    {
        var fullPath = ResolveSafePath(SubPath) ?? rootPath;
        Directory.CreateDirectory(fullPath);

        foreach (var file in files)
        {
            var dest = Path.Combine(fullPath, file.Name);
            await using var stream = new FileStream(dest, FileMode.Create, FileAccess.Write);
            await file.OpenReadStream(maxAllowedSize: 100 * 1024 * 1024).CopyToAsync(stream);
        }

        Snackbar.Add($"Uploaded {files.Count} file(s).", Severity.Success);
        await LoadEntriesAsync();
    }

    private async Task ShowNewFolderDialogAsync()
    {
        var parameters = new DialogParameters<TextInputDialog> { { x => x.Label, "Folder name" } };
        var dialog = await DialogService.ShowAsync<TextInputDialog>("New Folder", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false, Data: string name } && !string.IsNullOrWhiteSpace(name))
        {
            var fullPath = ResolveSafePath(SubPath) ?? rootPath;
            Directory.CreateDirectory(Path.Combine(fullPath, name));
            Snackbar.Add($"Folder '{name}' created.", Severity.Success);
            await LoadEntriesAsync();
        }
    }

    private async Task DeleteAsync(StorageEntry entry)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete", $"Delete '{entry.Name}'?", yesText: "Delete", cancelText: "Cancel");
        if (confirmed != true) return;

        var fullPath = ResolveSafePath(SubPath) ?? rootPath;
        var target = Path.Combine(fullPath, entry.Name);

        if (entry.IsDirectory)
            Directory.Delete(target, recursive: true);
        else
            File.Delete(target);

        Snackbar.Add($"'{entry.Name}' deleted.", Severity.Success);
        await LoadEntriesAsync();
    }

    private string GetDownloadUrl(StorageEntry entry)
    {
        var path = string.IsNullOrEmpty(SubPath) ? entry.Name : $"{SubPath}/{entry.Name}";
        return $"/api/storage/{path}";
    }

    private string? ResolveSafePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return rootPath;
        var combined = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        return !combined.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) ? null : combined;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):0.##} GB"
    };

    public sealed class StorageEntry
    {
        public string Name { get; init; } = string.Empty;
        public bool IsDirectory { get; init; }
        public bool IsParent { get; init; }
        public long Length { get; init; }
        public DateTime? LastModified { get; init; }
    }
}
