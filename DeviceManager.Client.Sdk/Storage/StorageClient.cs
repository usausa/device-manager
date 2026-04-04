namespace DeviceManager.Client.Sdk.Storage;

using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;

internal sealed class StorageClient : IStorageClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;

    public StorageClient(HttpClient httpClient, ILogger logger)
    {
        this.httpClient = httpClient;
    }

    public async Task UploadAsync(string path, Stream stream, CancellationToken cancellationToken = default)
    {
        var url = $"api/storage/{path.TrimStart('/')}";
        using var content = new StreamContent(stream);
        var response = await httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        var url = $"api/storage/{path.TrimStart('/')}";
        var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var url = $"api/storage/{path.TrimStart('/')}";
        var response = await httpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string[]> ListAsync(string path, CancellationToken cancellationToken = default)
    {
        var url = $"api/storage/{path.TrimStart('/')}/";
        var result = await httpClient.GetFromJsonAsync<string[]>(url, JsonOptions, cancellationToken).ConfigureAwait(false);
        return result ?? [];
    }
}
