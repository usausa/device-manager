namespace DeviceManager.Client.Sdk;

public interface IDeviceInfoProvider
{
    string DeviceId { get; }
    string DeviceName { get; }
    string Platform { get; }
    IDictionary<string, string>? AdditionalInfo { get; }
}
