namespace DeviceManager.Options;

/// <summary>Controls which columns are visible in the dashboard device table.</summary>
public sealed class DashboardOptions
{
    public bool ShowWifi { get; set; } = true;
    public bool ShowBattery { get; set; } = true;
    public bool ShowProgress1 { get; set; } = true;
    public bool ShowProgress2 { get; set; } = true;
    public bool ShowCpu { get; set; } = true;
    public bool ShowMemory { get; set; } = true;
    public bool ShowGps { get; set; } = true;
}
