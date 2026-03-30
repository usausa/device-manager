using System.Net.Http.Json;
using System.Text.Json;
using DeviceManager.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DeviceManager.Client.Sdk.DataStore;

internal sealed class DataStoreClient : IDataStoreClient
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string _deviceId;
    private readonly ILogger _logger;

    public DataStoreClient(HttpClient httpClient, string deviceId, ILogger logger)
    {
        _httpClient = httpClient;
        _deviceId = deviceId;
        _logger = logger;
    }

    public async Task<DataStoreEntry?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/devices/{Uri.EscapeDataString(_deviceId)}/{Uri.EscapeDataString(key)}";
        var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DataStoreEntry>(s_jsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DataStoreEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/devices/{Uri.EscapeDataString(_deviceId)}";
        var entries = await _httpClient.GetFromJsonAsync<List<DataStoreEntry>>(url, s_jsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return entries?.AsReadOnly() ?? (IReadOnlyList<DataStoreEntry>)[];
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/devices/{Uri.EscapeDataString(_deviceId)}/{Uri.EscapeDataString(key)}";
        var response = await _httpClient.PutAsJsonAsync(url, new { value }, s_jsonOptions, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/devices/{Uri.EscapeDataString(_deviceId)}/{Uri.EscapeDataString(key)}";
        var response = await _httpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<DataStoreEntry?> GetCommonAsync(string key, CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/common/{Uri.EscapeDataString(key)}";
        var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DataStoreEntry>(s_jsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DataStoreEntry>> GetAllCommonAsync(CancellationToken cancellationToken = default)
    {
        var url = "api/datastore/common";
        var entries = await _httpClient.GetFromJsonAsync<List<DataStoreEntry>>(url, s_jsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return entries?.AsReadOnly() ?? (IReadOnlyList<DataStoreEntry>)[];
    }

    public async Task SetCommonAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/common/{Uri.EscapeDataString(key)}";
        var response = await _httpClient.PutAsJsonAsync(url, new { value }, s_jsonOptions, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteCommonAsync(string key, CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/common/{Uri.EscapeDataString(key)}";
        var response = await _httpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
