namespace DeviceManager.Components.Pages;

using DeviceManager.Options;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

public partial class Dashboard : IDisposable
{
    [Inject]
    public DeviceService DeviceService { get; set; } = default!;

    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public DeviceEventService EventBus { get; set; } = default!;

    [Inject]
    public IOptions<DashboardOptions> DashboardOptionsAccessor { get; set; } = default!;

    private StatusSummary? summary;
    private List<DeviceSummary>? devices;
    private string searchText = string.Empty;

    private IDisposable? connectedSub;
    private IDisposable? disconnectedSub;
    private IDisposable? statusSub;
    private IDisposable? screenshotSub;

    private readonly Dictionary<string, bool> pendingScreenshot = [];

    internal DashboardOptions Options => DashboardOptionsAccessor.Value;

    private IEnumerable<DeviceSummary> FilteredDevices => devices ?? [];

    private bool DeviceFilter(DeviceSummary d) =>
        string.IsNullOrEmpty(searchText)
        || d.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
        || (d.Group?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
        || d.DeviceId.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();

        connectedSub = EventBus.Subscribe(AppEvents.DeviceConnected, _ => ReloadAsync());
        disconnectedSub = EventBus.Subscribe(AppEvents.DeviceDisconnected, _ => ReloadAsync());
        statusSub = EventBus.Subscribe(AppEvents.DeviceStatusUpdated, _ => ReloadAsync());
        screenshotSub = EventBus.Subscribe(AppEvents.ScreenshotReceived, async payload =>
        {
            if (payload is ScreenshotResult result)
            {
                pendingScreenshot.Remove(result.DeviceId);
                await ShowScreenshotDialogAsync(result);
            }
        });
    }

    private async Task ReloadAsync()
    {
        await LoadDataAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadDataAsync()
    {
        summary = await DeviceService.GetStatusSummaryAsync();
        devices = await DeviceService.GetAllDevicesAsync();
    }

    private void NavigateToDevice(string deviceId) => Navigation.NavigateTo($"/devices/{deviceId}");

    private async Task RequestScreenshotAsync(DeviceSummary device)
    {
        pendingScreenshot[device.DeviceId] = true;
        StateHasChanged();
        await EventBus.RequestScreenshotAsync(device.DeviceId);
        Snackbar.Add($"Screenshot requested from {device.Name}.", Severity.Info);
    }

    private async Task ShowScreenshotDialogAsync(ScreenshotResult result)
    {
        var parameters = new DialogParameters<ScreenshotDialog>
        {
            { x => x.Result, result }
        };
        await InvokeAsync(async () =>
        {
            await DialogService.ShowAsync<ScreenshotDialog>(
                $"Screenshot — {result.DeviceId}", parameters,
                new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true });
            StateHasChanged();
        });
    }

    private async Task ShowAddDialog()
    {
        var dialog = await DialogService.ShowAsync<DeviceEditDialog>("Add Device");
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await LoadDataAsync();
        }
    }

    private async Task ShowEditDialogAsync(string deviceId)
    {
        var parameters = new DialogParameters<DeviceEditDialog>
        {
            { x => x.DeviceId, deviceId }
        };
        var dialog = await DialogService.ShowAsync<DeviceEditDialog>("Edit Device", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await LoadDataAsync();
        }
    }

    private async Task ConfirmDeleteAsync(DeviceSummary device)
    {
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete Device", $"Delete device '{device.Name}'?", yesText: "Delete", cancelText: "Cancel");
        if (confirmed != true)
        {
            return;
        }

        await DeviceService.DeleteDeviceAsync(device.DeviceId);
        Snackbar.Add($"Device '{device.Name}' deleted.", Severity.Success);
        await LoadDataAsync();
    }

    internal static Color GetStatusColor(DeviceConnectionStatus status) => status switch
    {
        DeviceConnectionStatus.Active => Color.Success,
        DeviceConnectionStatus.Warning => Color.Warning,
        DeviceConnectionStatus.Error => Color.Error,
        _ => Color.Default
    };

    internal static string GetWifiIcon(int rssi) => rssi switch
    {
        > -50 => Icons.Material.Filled.Wifi,
        > -70 => Icons.Material.Filled.Wifi2Bar,
        _ => Icons.Material.Filled.Wifi1Bar
    };

    internal static Color GetWifiColor(int rssi) => rssi switch
    {
        > -50 => Color.Success,
        > -70 => Color.Warning,
        _ => Color.Error
    };

    internal static string GetBatteryIcon(int battery) => battery switch
    {
        > 80 => Icons.Material.Filled.BatteryFull,
        > 60 => Icons.Material.Filled.Battery6Bar,
        > 40 => Icons.Material.Filled.Battery4Bar,
        > 20 => Icons.Material.Filled.Battery2Bar,
        _ => Icons.Material.Filled.BatteryAlert
    };

    internal static Color GetBatteryColor(int battery) => battery switch
    {
        > 50 => Color.Success,
        > 20 => Color.Warning,
        _ => Color.Error
    };

    internal static Color GetUsageColor(double usage) => usage switch
    {
        < 60 => Color.Success,
        < 85 => Color.Warning,
        _ => Color.Error
    };

    private static string FormatDateTime(DateTime? dt)
    {
        if (dt is null)
        {
            return "-";
        }

        var local = dt.Value.ToLocalTime();
        var diff = DateTime.Now - local;
        if (diff.TotalMinutes < 1)
        {
            return "Just now";
        }

        if (diff.TotalMinutes < 60)
        {
            return $"{(int)diff.TotalMinutes}m ago";
        }

        if (diff.TotalHours < 24)
        {
            return $"{(int)diff.TotalHours}h ago";
        }
        return local.ToString("MM/dd HH:mm");
    }

    public void Dispose()
    {
        connectedSub?.Dispose();
        disconnectedSub?.Dispose();
        statusSub?.Dispose();
        screenshotSub?.Dispose();
    }
}
