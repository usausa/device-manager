using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using DeviceManager.Client.Sdk.Connection;
using DeviceManager.Shared;
using DeviceManager.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace DeviceManager.Client.Sdk.Config;

internal sealed class ConfigManager : IConfigManager, IDisposable
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string _deviceId;
    private readonly string? _cachePath;
    private readonly SignalRConnectionManager? _signalRConnectionManager;
    private readonly GrpcConnectionManager? _grpcConnectionManager;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, ConfigEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IDisposable> _subscriptions = [];

    public event EventHandler<ConfigEntry>? ConfigChanged;

    public ConfigManager(
        HttpClient httpClient,
        string deviceId,
        string? cachePath,
        SignalRConnectionManager? signalRConnectionManager,
        GrpcConnectionManager? grpcConnectionManager,
        ILogger logger)
    {
        _httpClient = httpClient;
        _deviceId = deviceId;
        _cachePath = cachePath;
        _signalRConnectionManager = signalRConnectionManager;
        _grpcConnectionManager = grpcConnectionManager;
        _logger = logger;

        LoadCacheFromDisk();

        if (_signalRConnectionManager is not null)
        {
            _signalRConnectionManager.ConnectionStateChanged += OnConnectionStateChanged;
        }

        if (_grpcConnectionManager is not null)
        {
            _grpcConnectionManager.ConfigUpdated += OnGrpcConfigUpdated;
        }
    }

    public async Task<IReadOnlyList<ConfigEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await FetchAndCacheAllAsync(cancellationToken).ConfigureAwait(false);
        return _cache.Values.ToList().AsReadOnly();
    }

    public async Task<ConfigEntry?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        await FetchAndCacheAllAsync(cancellationToken).ConfigureAwait(false);
        return _cache.GetValueOrDefault(key);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await FetchAndCacheAllAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task FetchAndCacheAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var url = $"api/config/devices/{Uri.EscapeDataString(_deviceId)}/resolved";
            var entries = await _httpClient.GetFromJsonAsync<List<ConfigEntry>>(url, s_jsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (entries is not null)
            {
                _cache.Clear();
                foreach (var entry in entries)
                {
                    _cache[entry.Key] = entry;
                }

                SaveCacheToDisk();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch config from server; using cached values");
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
        _cache[entry.Key] = entry;
        SaveCacheToDisk();
        ConfigChanged?.Invoke(this, entry);
    }

    private void RegisterHubHandlers()
    {
        var hub = _signalRConnectionManager?.HubConnection;
        if (hub is null) return;

        foreach (var sub in _subscriptions) sub.Dispose();
        _subscriptions.Clear();

        _subscriptions.Add(hub.On<List<ConfigEntry>>(HubConstants.ServerMethods.ConfigReload, entries =>
        {
            _cache.Clear();
            foreach (var entry in entries)
            {
                _cache[entry.Key] = entry;
            }

            SaveCacheToDisk();
        }));
    }

    private void LoadCacheFromDisk()
    {
        if (_cachePath is null || !File.Exists(_cachePath)) return;

        try
        {
            var json = File.ReadAllText(_cachePath);
            var entries = JsonSerializer.Deserialize<List<ConfigEntry>>(json, s_jsonOptions);
            if (entries is not null)
            {
                foreach (var entry in entries) _cache[entry.Key] = entry;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load config cache from disk");
        }
    }

    private void SaveCacheToDisk()
    {
        if (_cachePath is null) return;

        try
        {
            var directory = Path.GetDirectoryName(_cachePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(_cache.Values.ToList(), s_jsonOptions);
            File.WriteAllText(_cachePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save config cache to disk");
        }
    }

    public void Dispose()
    {
        if (_signalRConnectionManager is not null)
        {
            _signalRConnectionManager.ConnectionStateChanged -= OnConnectionStateChanged;
        }

        if (_grpcConnectionManager is not null)
        {
            _grpcConnectionManager.ConfigUpdated -= OnGrpcConfigUpdated;
        }

        foreach (var sub in _subscriptions) sub.Dispose();
        _subscriptions.Clear();
    }
}
