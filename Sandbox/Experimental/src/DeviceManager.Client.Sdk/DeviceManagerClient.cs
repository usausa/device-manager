using DeviceManager.Client.Sdk.Config;
using DeviceManager.Client.Sdk.Connection;
using DeviceManager.Client.Sdk.DataStore;
using DeviceManager.Client.Sdk.Messaging;
using DeviceManager.Client.Sdk.Storage;
using DeviceManager.Shared;
using DeviceManager.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DeviceManager.Client.Sdk;

public sealed class DeviceManagerClient : IAsyncDisposable
{
    private readonly SignalRConnectionManager? _signalRConnectionManager;
    private readonly GrpcConnectionManager? _grpcConnectionManager;
    private readonly bool _useGrpc;
    private readonly ConfigManager _configManager;
    private readonly DataStoreClient _dataStoreClient;
    private readonly MessageClient _messageClient;
    private readonly StorageClient _storageClient;
    private readonly IDeviceStatusProvider? _statusProvider;
    private readonly IDeviceInfoProvider _deviceInfoProvider;
    private readonly ILogger<DeviceManagerClient> _logger;

    private PeriodicTimer? _statusTimer;
    private CancellationTokenSource? _statusCts;
    private Task? _statusTask;
    private bool _disposed;

    public DeviceManagerClient(
        DeviceManagerClientOptions options,
        IDeviceInfoProvider deviceInfoProvider,
        ILoggerFactory? loggerFactory = null,
        HttpClient? httpClient = null,
        IDeviceStatusProvider? statusProvider = null,
        IDeviceCommandHandler? commandHandler = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(deviceInfoProvider);

        loggerFactory ??= NullLoggerFactory.Instance;
        _logger = loggerFactory.CreateLogger<DeviceManagerClient>();

        httpClient ??= CreateDefaultHttpClient(options);

        _statusProvider = statusProvider;
        _deviceInfoProvider = deviceInfoProvider;
        _useGrpc = options.UseGrpc;

        if (_useGrpc)
        {
            _grpcConnectionManager = new GrpcConnectionManager(
                options,
                deviceInfoProvider,
                commandHandler,
                loggerFactory.CreateLogger<GrpcConnectionManager>());

            _grpcConnectionManager.ConnectionStateChanged += (_, state) => ConnectionStateChanged?.Invoke(this, state);
        }
        else
        {
            _signalRConnectionManager = new SignalRConnectionManager(
                options,
                deviceInfoProvider,
                commandHandler,
                loggerFactory.CreateLogger<SignalRConnectionManager>());

            _signalRConnectionManager.ConnectionStateChanged += (_, state) => ConnectionStateChanged?.Invoke(this, state);
        }

        _configManager = new ConfigManager(
            httpClient,
            deviceInfoProvider.DeviceId,
            options.ConfigCachePath,
            _signalRConnectionManager,
            _grpcConnectionManager,
            loggerFactory.CreateLogger<ConfigManager>());

        _dataStoreClient = new DataStoreClient(
            httpClient,
            deviceInfoProvider.DeviceId,
            loggerFactory.CreateLogger<DataStoreClient>());

        _messageClient = new MessageClient(
            _signalRConnectionManager,
            _grpcConnectionManager,
            loggerFactory.CreateLogger<MessageClient>());

        _storageClient = new StorageClient(
            httpClient,
            loggerFactory.CreateLogger<StorageClient>());
    }

    public event EventHandler<ConnectionState>? ConnectionStateChanged;

    public ConnectionState State => _useGrpc
        ? _grpcConnectionManager!.State
        : _signalRConnectionManager!.State;

    public IConfigManager Config => _configManager;
    public IDataStoreClient DataStore => _dataStoreClient;
    public IMessageClient Messages => _messageClient;
    public IStorageClient Storage => _storageClient;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_useGrpc)
            return _grpcConnectionManager!.ConnectAsync(cancellationToken);

        return _signalRConnectionManager!.ConnectAsync(cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await StopStatusReportingAsync().ConfigureAwait(false);

        if (_useGrpc)
            await _grpcConnectionManager!.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        else
            await _signalRConnectionManager!.DisconnectAsync(cancellationToken).ConfigureAwait(false);
    }

    public void StartStatusReporting(TimeSpan? interval = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_statusProvider is null)
        {
            throw new InvalidOperationException(
                "Cannot start status reporting without an IDeviceStatusProvider.");
        }

        if (_statusTask is not null)
        {
            _logger.LogWarning("Status reporting is already running");
            return;
        }

        var reportInterval = interval ?? TimeSpan.FromSeconds(30);
        _statusCts = new CancellationTokenSource();
        _statusTimer = new PeriodicTimer(reportInterval);
        _statusTask = RunStatusReportingLoopAsync(_statusCts.Token);

        _logger.LogInformation("Status reporting started with interval {Interval}", reportInterval);
    }

    public async Task StopStatusReportingAsync()
    {
        if (_statusTask is null) return;

        _statusCts?.Cancel();

        try
        {
            await _statusTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        finally
        {
            _statusTimer?.Dispose();
            _statusCts?.Dispose();
            _statusTimer = null;
            _statusCts = null;
            _statusTask = null;
            _logger.LogInformation("Status reporting stopped");
        }
    }

    private async Task RunStatusReportingLoopAsync(CancellationToken cancellationToken)
    {
        while (await _statusTimer!.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                var currentState = _useGrpc
                    ? _grpcConnectionManager!.State
                    : _signalRConnectionManager!.State;

                if (currentState != ConnectionState.Connected) continue;

                var status = await _statusProvider!.GetCurrentStatusAsync(cancellationToken).ConfigureAwait(false);

                if (_useGrpc)
                {
                    await _grpcConnectionManager!.ReportStatusAsync(status, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var hub = _signalRConnectionManager!.HubConnection;
                    if (hub is null) continue;

                    await hub.InvokeAsync(
                        HubConstants.ClientMethods.ReportStatus,
                        status,
                        cancellationToken).ConfigureAwait(false);
                }

                _logger.LogDebug("Status report sent");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send status report");
            }
        }
    }

    private static HttpClient CreateDefaultHttpClient(DeviceManagerClientOptions options)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(options.ServerUrl.TrimEnd('/') + "/"),
            Timeout = options.ApiTimeout
        };
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await StopStatusReportingAsync().ConfigureAwait(false);
        _messageClient.Dispose();
        _configManager.Dispose();

        if (_grpcConnectionManager is not null)
            await _grpcConnectionManager.DisposeAsync().ConfigureAwait(false);

        if (_signalRConnectionManager is not null)
            await _signalRConnectionManager.DisposeAsync().ConfigureAwait(false);
    }
}
