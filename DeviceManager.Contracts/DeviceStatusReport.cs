namespace DeviceManager.Contracts;

/// <summary>
/// 端末のステータス報告。
/// </summary>
public sealed class DeviceStatusReport
{
    /// <summary>状態レベル(0:正常 / 1:警告 / 2:異常)。</summary>
    [Range(0, 2)]
    public int Level { get; set; }

    /// <summary>バッテリー残量(%)。</summary>
    [Range(0, 100)]
    public double Battery { get; set; }

    /// <summary>電波強度(RSSI dBm)。</summary>
    public int WifiRssi { get; set; }

    /// <summary>接続中のアクセスポイント名。</summary>
    [MaxLength(100)]
    public string? ApName { get; set; }

    /// <summary>移動中か。</summary>
    public bool Moving { get; set; }

    /// <summary>スキャン件数。</summary>
    public int ScanCount { get; set; }

    /// <summary>進捗1(%)。</summary>
    [Range(0, 100)]
    public double Progress1 { get; set; }

    /// <summary>進捗2(%)。</summary>
    [Range(0, 100)]
    public double Progress2 { get; set; }

    /// <summary>緯度。</summary>
    public double? Latitude { get; set; }

    /// <summary>経度。</summary>
    public double? Longitude { get; set; }
}
