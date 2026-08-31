namespace DeviceManager.Models.Entity;

public sealed class DeviceStatusEntity
{
    public string DeviceId { get; set; } = default!;

    // 0:正常 / 1:警告 / 2:異常
    public int Level { get; set; }

    public double Battery { get; set; }

    public int WifiRssi { get; set; }

    public string? ApName { get; set; }

    // 0:停止 / 1:移動中
    public int Moving { get; set; }

    public int ScanCount { get; set; }

    public double Progress1 { get; set; }

    public double Progress2 { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public DateTime UpdatedAt { get; set; }
}
