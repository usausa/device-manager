namespace DeviceManager.Client.Sdk.Config;

using DeviceManager.Shared.Models;

public interface IConfigManager
{
    event EventHandler<ConfigEntry>? ConfigChanged;
    Task<IReadOnlyList<ConfigEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ConfigEntry?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
