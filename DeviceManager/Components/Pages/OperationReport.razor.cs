namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;

public partial class OperationReport
{
    [Inject]
    public DeviceService DeviceService { get; set; } = default!;

    private List<DeviceSummary> allDevices = [];
    private string? selectedDeviceId;
    private DeviceDetail? device;
    private List<ConnectionEvent> connectionLog = [];
    private List<DeviceStatusReport> statusHistory = [];
    private bool isLoading;

    private int totalConnections;
    private TimeSpan estimatedUptime;
    private double? avgCpu;
    private double? avgMemory;
    private double? avgBattery;

    protected override async Task OnInitializedAsync()
    {
        allDevices = await DeviceService.GetAllDevicesAsync();
    }

    internal async Task LoadReportAsync()
    {
        if (string.IsNullOrEmpty(selectedDeviceId))
        {
            return;
        }

        isLoading = true;
        StateHasChanged();

        device = await DeviceService.GetDeviceAsync(selectedDeviceId);
        connectionLog = await DeviceService.GetConnectionLogAsync(selectedDeviceId, 50);
        statusHistory = await DeviceService.GetStatusHistoryAsync(selectedDeviceId);
        statusHistory = [.. statusHistory.Take(50).OrderBy(x => x.Timestamp)];

        ComputeStats();

        isLoading = false;
    }

    private void ComputeStats()
    {
        totalConnections = connectionLog.Count(e => e.Connected);

        // Estimate uptime from paired connect/disconnect events
        var uptime = TimeSpan.Zero;
        var sorted = connectionLog.OrderBy(e => e.Timestamp).ToList();
        DateTime? connectTime = null;
        foreach (var ev in sorted)
        {
            if (ev.Connected)
            {
                connectTime = ev.Timestamp;
            }
            else if (connectTime.HasValue)
            {
                uptime += ev.Timestamp - connectTime.Value;
                connectTime = null;
            }
        }

        if (connectTime.HasValue)
        {
            uptime += DateTime.UtcNow - connectTime.Value;
        }

        estimatedUptime = uptime;

        var cpuValues = statusHistory.Where(h => h.CpuUsage.HasValue).Select(h => h.CpuUsage!.Value).ToList();
        var memValues = statusHistory.Where(h => h.MemoryUsage.HasValue).Select(h => h.MemoryUsage!.Value).ToList();
        var batValues = statusHistory.Where(h => h.Battery.HasValue).Select(h => (double)h.Battery!.Value).ToList();

        avgCpu = cpuValues.Count > 0 ? cpuValues.Average() : null;
        avgMemory = memValues.Count > 0 ? memValues.Average() : null;
        avgBattery = batValues.Count > 0 ? batValues.Average() : null;
    }

    private static Color GetStatusColor(DeviceConnectionStatus status) => status switch
    {
        DeviceConnectionStatus.Active => Color.Success,
        DeviceConnectionStatus.Warning => Color.Warning,
        DeviceConnectionStatus.Error => Color.Error,
        _ => Color.Default
    };

    private static Color GetUsageColor(double usage) => usage switch
    {
        < 60 => Color.Success,
        < 85 => Color.Warning,
        _ => Color.Error
    };

    private static Color GetBatteryColor(int battery) => battery switch
    {
        > 50 => Color.Success,
        > 20 => Color.Warning,
        _ => Color.Error
    };

    private static string FormatUptime(TimeSpan ts)
    {
        if (ts.TotalSeconds < 60)
        {
            return $"{(int)ts.TotalSeconds}s";
        }

        if (ts.TotalMinutes < 60)
        {
            return $"{(int)ts.TotalMinutes}m";
        }

        if (ts.TotalHours < 24)
        {
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        }

        return $"{(int)ts.TotalDays}d {ts.Hours}h";
    }
}
