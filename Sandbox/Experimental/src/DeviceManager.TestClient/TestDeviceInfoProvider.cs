using DeviceManager.Client.Sdk;

namespace DeviceManager.TestClient;

public class TestDeviceInfoProvider : IDeviceInfoProvider
{
    public string DeviceId { get; set; } = $"test-{Environment.MachineName.ToLower()}";
    public string DeviceName { get; set; } = $"TestClient-{Environment.MachineName}";
    public string Platform => "WPF-Windows";
    public IDictionary<string, string>? AdditionalInfo => new Dictionary<string, string>
    {
        ["os"] = Environment.OSVersion.ToString(),
        ["runtime"] = Environment.Version.ToString()
    };
}
