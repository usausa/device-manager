namespace DeviceManager.TestClient;

using DeviceManager.Client.Sdk;
using DeviceManager.Shared.Models;

internal sealed class TestDeviceInfoProvider : IDeviceInfoProvider
{
    public string DeviceId { get; } = $"test-device-{Environment.MachineName.ToLowerInvariant()}";
    public string DeviceName { get; } = $"Test Device ({Environment.MachineName})";
    public string Platform { get; } = $"{Environment.OSVersion.Platform}";
    public IDictionary<string, string>? AdditionalInfo { get; } = new Dictionary<string, string>
    {
        ["runtime"] = Environment.Version.ToString(),
        ["os"] = Environment.OSVersion.VersionString
    };
}
