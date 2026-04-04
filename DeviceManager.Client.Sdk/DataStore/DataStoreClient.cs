namespace DeviceManager.Client.Sdk.DataStore;

using System.Net.Http.Json;
using System.Text.Json;

using DeviceManager.Shared.Models;

using Microsoft.Extensions.Logging;

internal sealed class DataStoreClient : IDataStoreClient
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly string deviceId;

    public DataStoreClient(HttpClient httpClient, string deviceId, ILogger logger)
    {
        this.httpClient = httpClient;
        this.deviceId = deviceId;
    }

    public async Task<DataStoreEntry?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/devices/{Uri.EscapeDataString(deviceId)}/{Uri.EscapeDataString(key)}";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DataStoreEntry>(s_jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DataStoreEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/devices/{Uri.EscapeDataString(deviceId)}";
        var entries = await httpClient.GetFromJsonAsync<List<DataStoreEntry>>(url, s_jsonOptions, cancellationToken).ConfigureAwait(false);
        return entries?.AsReadOnly() ?? (IReadOnlyList<DataStoreEntry>)[];
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/devices/{Uri.EscapeDataString(deviceId)}/{Uri.EscapeDataString(key)}";
        var response = await httpClient.PutAsJsonAsync(url, new { value }, s_jsonOptions, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/devices/{Uri.EscapeDataString(deviceId)}/{Uri.EscapeDataString(key)}";
        var response = await httpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<DataStoreEntry?> GetCommonAsync(string key, CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/common/{Uri.EscapeDataString(key)}";
        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DataStoreEntry>(s_jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DataStoreEntry>> GetAllCommonAsync(CancellationToken cancellationToken = default)
    {
        var url = "api/datastore/common";
        var entries = await httpClient.GetFromJsonAsync<List<DataStoreEntry>>(url, s_jsonOptions, cancellationToken).ConfigureAwait(false);
        return entries?.AsReadOnly() ?? (IReadOnlyList<DataStoreEntry>)[];
    }

    public async Task SetCommonAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/common/{Uri.EscapeDataString(key)}";
        var response = await httpClient.PutAsJsonAsync(url, new { value }, s_jsonOptions, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteCommonAsync(string key, CancellationToken cancellationToken = default)
    {
        var url = $"api/datastore/common/{Uri.EscapeDataString(key)}";
        var response = await httpClient.DeleteAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
