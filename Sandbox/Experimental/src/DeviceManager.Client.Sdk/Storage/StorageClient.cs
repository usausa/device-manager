using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DeviceManager.Client.Sdk.Storage;

internal sealed class StorageClient : IStorageClient
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public StorageClient(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task UploadAsync(string path, Stream stream, CancellationToken cancellationToken = default)
    {
        var url = $"api/storage/{path.TrimStart('/')}";
        using var content = new StreamContent(stream);
        var response = await _httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        _logger.LogDebug("Uploaded file to {Path}", path);
    }

    public async Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        var url = $"api/storage/{path.TrimStart('/')}";
        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var url = $"api/storage/{path.TrimStart('/')}";
        var response = await _httpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        _logger.LogDebug("Deleted file at {Path}", path);
    }

    public async Task<string[]> ListAsync(string path, CancellationToken cancellationToken = default)
    {
        var url = $"api/storage/{path.TrimStart('/')}/";
        var result = await _httpClient.GetFromJsonAsync<string[]>(url, s_jsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return result ?? [];
    }
}
