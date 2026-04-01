using DeviceManager.Shared.Models;

namespace DeviceManager.Client.Sdk.Config;

public interface IConfigManager
{
    event EventHandler<ConfigEntry>? ConfigChanged;
    Task<IReadOnlyList<ConfigEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ConfigEntry?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
