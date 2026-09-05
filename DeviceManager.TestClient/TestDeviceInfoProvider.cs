namespace DeviceManager.TestClient;

// テスト用の端末情報プロバイダー
public sealed class TestDeviceInfoProvider : IDeviceInfoProvider
{
    public string DeviceId { get; }

    public string DeviceName { get; }

    public string Platform => "Windows";

    public TestDeviceInfoProvider(string deviceId, string deviceName)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
    }
}
