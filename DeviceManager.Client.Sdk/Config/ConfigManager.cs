namespace DeviceManager.Client.Sdk.Config;

using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

using DeviceManager.Client.Sdk.Connection;
using DeviceManager.Shared;
using DeviceManager.Shared.Models;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

internal sealed class ConfigManager : IConfigManager, IDisposable
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly string deviceId;
    private readonly string? cachePath;
    private readonly SignalRConnectionManager? signalRConnectionManager;
    private readonly GrpcConnectionManager? grpcConnectionManager;
    private readonly ILogger logger;
    private readonly ConcurrentDictionary<string, ConfigEntry> cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IDisposable> subscriptions = [];

    public event EventHandler<ConfigEntry>? ConfigChanged;

    public ConfigManager(
        HttpClient httpClient,
        string deviceId,
        string? cachePath,
        SignalRConnectionManager? signalRConnectionManager,
        GrpcConnectionManager? grpcConnectionManager,
        ILogger logger)
    {
        this.httpClient = httpClient;
        this.deviceId = deviceId;
        this.cachePath = cachePath;
        this.signalRConnectionManager = signalRConnectionManager;
        this.grpcConnectionManager = grpcConnectionManager;
        this.logger = logger;

        LoadCacheFromDisk();

        if (this.signalRConnectionManager is not null)
        {
            this.signalRConnectionManager.ConnectionStateChanged += OnConnectionStateChanged;
        }

        if (this.grpcConnectionManager is not null)
        {
            this.grpcConnectionManager.ConfigUpdated += OnGrpcConfigUpdated;
        }
    }

    public async Task<IReadOnlyList<ConfigEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await FetchAndCacheAllAsync(cancellationToken).ConfigureAwait(false);
        return cache.Values.ToList().AsReadOnly();
    }

    public async Task<ConfigEntry?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        await FetchAndCacheAllAsync(cancellationToken).ConfigureAwait(false);
        return cache.GetValueOrDefault(key);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await FetchAndCacheAllAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task FetchAndCacheAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var url = $"api/config/devices/{Uri.EscapeDataString(deviceId)}/resolved";
            var entries = await httpClient.GetFromJsonAsync<List<ConfigEntry>>(url, s_jsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (entries is not null)
            {
                cache.Clear();
                foreach (var entry in entries)
                {
                    cache[entry.Key] = entry;
                }

                SaveCacheToDisk();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch config from server; using cached values");
        }
    }

    private void OnConnectionStateChanged(object? sender, ConnectionState state)
    {
        if (state == ConnectionState.Connected)
        {
            RegisterHubHandlers();
        }
    }

    private void OnGrpcConfigUpdated(object? sender, ConfigEntry entry)
    {
        cache[entry.Key] = entry;
        SaveCacheToDisk();
        ConfigChanged?.Invoke(this, entry);
    }

    private void RegisterHubHandlers()
    {
        var hub = signalRConnectionManager?.HubConnection;
        if (hub is null) return;

        foreach (var sub in subscriptions) sub.Dispose();
        subscriptions.Clear();

        subscriptions.Add(hub.On<List<ConfigEntry>>(HubConstants.ServerMethods.ConfigReload, entries =>
        {
            cache.Clear();
            foreach (var entry in entries)
            {
                cache[entry.Key] = entry;
            }

            SaveCacheToDisk();
        }));
    }

    private void LoadCacheFromDisk()
    {
        if (cachePath is null || !File.Exists(cachePath)) return;

        try
        {
            var json = File.ReadAllText(cachePath);
            var entries = JsonSerializer.Deserialize<List<ConfigEntry>>(json, s_jsonOptions);
            if (entries is not null)
            {
                foreach (var entry in entries) cache[entry.Key] = entry;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load config cache from disk");
        }
    }

    private void SaveCacheToDisk()
    {
        if (cachePath is null) return;

        try
        {
            var directory = Path.GetDirectoryName(cachePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(cache.Values.ToList(), s_jsonOptions);
            File.WriteAllText(cachePath, json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save config cache to disk");
        }
    }

    public void Dispose()
    {
        if (signalRConnectionManager is not null)
        {
            signalRConnectionManager.ConnectionStateChanged -= OnConnectionStateChanged;
        }

        if (grpcConnectionManager is not null)
        {
            grpcConnectionManager.ConfigUpdated -= OnGrpcConfigUpdated;
        }

        foreach (var sub in subscriptions) sub.Dispose();
        subscriptions.Clear();
    }
}
