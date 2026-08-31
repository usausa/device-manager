namespace DeviceManager.TestClient;

using System.Security.Cryptography;

// テスト用のランダムステータスプロバイダー
public sealed class RandomStatusProvider : IDeviceStatusProvider
{
    private static readonly string[] ApNames = ["AP-FLOOR1", "AP-FLOOR2", "AP-WAREHOUSE"];

    private int scanCount;

    private double progress1;

    private double progress2;

    public ValueTask<DeviceStatusReport> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        scanCount += RandomNumberGenerator.GetInt32(0, 5);
        progress1 = (progress1 + RandomNumberGenerator.GetInt32(0, 10)) % 100;
        progress2 = (progress2 + RandomNumberGenerator.GetInt32(0, 5)) % 100;

        // まれに警告・異常レベルを混ぜる
        var sample = RandomNumberGenerator.GetInt32(0, 100);
        var level = sample switch
        {
            >= 95 => 2,
            >= 80 => 1,
            _ => 0
        };

        var report = new DeviceStatusReport
        {
            Level = level,
            Battery = RandomNumberGenerator.GetInt32(15, 101),
            WifiRssi = -RandomNumberGenerator.GetInt32(40, 91),
            ApName = ApNames[RandomNumberGenerator.GetInt32(0, ApNames.Length)],
            Moving = RandomNumberGenerator.GetInt32(0, 2) == 1,
            ScanCount = scanCount,
            Progress1 = progress1,
            Progress2 = progress2,
            Latitude = 35.68 + (RandomNumberGenerator.GetInt32(0, 1000) / 100000d),
            Longitude = 139.76 + (RandomNumberGenerator.GetInt32(0, 1000) / 100000d)
        };
        return ValueTask.FromResult(report);
    }
}
