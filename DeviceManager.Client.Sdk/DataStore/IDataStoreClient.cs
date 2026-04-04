namespace DeviceManager.Client.Sdk.DataStore;

using DeviceManager.Shared.Models;

public interface IDataStoreClient
{
    Task<DataStoreEntry?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataStoreEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    Task<DataStoreEntry?> GetCommonAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataStoreEntry>> GetAllCommonAsync(CancellationToken cancellationToken = default);
    Task SetCommonAsync(string key, string value, CancellationToken cancellationToken = default);
    Task DeleteCommonAsync(string key, CancellationToken cancellationToken = default);
}
