namespace DeviceManager.Models;

// ダッシュボード一覧用のビュー(Device + DeviceStatus + ログ件数)
public sealed class DeviceListView
{
    public string DeviceId { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string? GroupName { get; set; }

    public string? Note { get; set; }

    public int IsEnabled { get; set; }

    public int Status { get; set; }

    public DateTime? LastConnectedAt { get; set; }

    public double? Battery { get; set; }

    public int? WifiRssi { get; set; }

    public string? ApName { get; set; }

    public int? Moving { get; set; }

    public int? ScanCount { get; set; }

    public double? Progress1 { get; set; }

    public double? Progress2 { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public DateTime? StatusUpdatedAt { get; set; }

    public int LogCount { get; set; }
}
