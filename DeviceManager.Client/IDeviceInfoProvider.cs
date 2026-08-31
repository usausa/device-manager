namespace DeviceManager.Client;

// 端末固有情報の提供(プラットフォーム依存部を SDK 外へ追い出すためのインターフェース)
public interface IDeviceInfoProvider
{
    string DeviceId { get; }

    string DeviceName { get; }

    string? Platform { get; }
}
