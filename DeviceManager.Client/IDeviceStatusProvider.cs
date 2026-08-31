namespace DeviceManager.Client;

// ステータス値の提供(取得方法は利用側が実装する)
public interface IDeviceStatusProvider
{
    ValueTask<DeviceStatusReport> GetStatusAsync(CancellationToken cancellationToken = default);
}
