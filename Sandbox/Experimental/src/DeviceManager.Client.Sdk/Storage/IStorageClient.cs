namespace DeviceManager.Client.Sdk.Storage;

public interface IStorageClient
{
    Task UploadAsync(string path, Stream stream, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
    Task<string[]> ListAsync(string path, CancellationToken cancellationToken = default);
}
