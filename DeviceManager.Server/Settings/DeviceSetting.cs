namespace DeviceManager.Server.Settings;

public sealed class DeviceSetting
{
    // 端末 API / ハブの共有 API キー
    [Required]
    [MinLength(16)]
    public string ApiKey { get; set; } = default!;

    // バッテリー低下と判定する閾値(%)
    [Range(1, 100)]
    public double LowBatteryThreshold { get; set; } = 20;
}
