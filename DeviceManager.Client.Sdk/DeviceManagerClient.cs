namespace DeviceManager.Client.Sdk;

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

public sealed class DeviceManagerClient : IAsyncDisposable
{
    private readonly SignalRConnectionManager? signalRConnectionManager;
    private readonly GrpcConnectionManager? grpcConnectionManager;
    private readonly bool useGrpc;
    private readonly ConfigManager configManager;
    private readonly DataStoreClient dataStoreClient;
    private readonly MessageClient messageClient;
    private readonly StorageClient storageClient;
    private readonly IDeviceStatusProvider? statusProvider;
    private readonly ILogger<DeviceManagerClient> logger;

    private PeriodicTimer? statusTimer;
    private CancellationTokenSource? statusCts;
    private Task? statusTask;
    private bool disposed;

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
        logger = loggerFactory.CreateLogger<DeviceManagerClient>();

        httpClient ??= CreateDefaultHttpClient(options);

        this.statusProvider = statusProvider;
        useGrpc = options.UseGrpc;

        if (useGrpc)
        {
            grpcConnectionManager = new GrpcConnectionManager(
                options, deviceInfoProvider, commandHandler,
                loggerFactory.CreateLogger<GrpcConnectionManager>());
            grpcConnectionManager.ConnectionStateChanged += (_, state) => ConnectionStateChanged?.Invoke(this, state);
        }
        else
        {
            signalRConnectionManager = new SignalRConnectionManager(
                options, deviceInfoProvider, commandHandler,
                loggerFactory.CreateLogger<SignalRConnectionManager>());
            signalRConnectionManager.ConnectionStateChanged += (_, state) => ConnectionStateChanged?.Invoke(this, state);
        }

        configManager = new ConfigManager(
            httpClient, deviceInfoProvider.DeviceId, options.ConfigCachePath,
            signalRConnectionManager, grpcConnectionManager,
            loggerFactory.CreateLogger<ConfigManager>());

        dataStoreClient = new DataStoreClient(
            httpClient, deviceInfoProvider.DeviceId,
            loggerFactory.CreateLogger<DataStoreClient>());

        messageClient = new MessageClient(
            signalRConnectionManager, grpcConnectionManager,
            loggerFactory.CreateLogger<MessageClient>());

        storageClient = new StorageClient(
            httpClient, loggerFactory.CreateLogger<StorageClient>());
    }

    public event EventHandler<ConnectionState>? ConnectionStateChanged;

    public ConnectionState State => useGrpc
        ? grpcConnectionManager!.State
        : signalRConnectionManager!.State;

    public IConfigManager Config => configManager;
    public IDataStoreClient DataStore => dataStoreClient;
    public IMessageClient Messages => messageClient;
    public IStorageClient Storage => storageClient;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return useGrpc
            ? grpcConnectionManager!.ConnectAsync(cancellationToken)
            : signalRConnectionManager!.ConnectAsync(cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await StopStatusReportingAsync().ConfigureAwait(false);

        if (useGrpc)
        {
            await grpcConnectionManager!.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await signalRConnectionManager!.DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public void StartStatusReporting(TimeSpan? interval = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (statusProvider is null)
        {
            throw new InvalidOperationException("Cannot start status reporting without an IDeviceStatusProvider.");
        }

        if (statusTask is not null)
        {
            logger.LogWarning("Status reporting is already running");
            return;
        }

        var reportInterval = interval ?? TimeSpan.FromSeconds(30);
        statusCts = new CancellationTokenSource();
        statusTimer = new PeriodicTimer(reportInterval);
        statusTask = RunStatusReportingLoopAsync(statusCts.Token);
        logger.LogInformation("Status reporting started with interval {Interval}", reportInterval);
    }

    public async Task StopStatusReportingAsync()
    {
        if (statusTask is null)
        {
            return;
        }

        statusCts?.Cancel();

        try
        {
            await statusTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            statusTimer?.Dispose();
            statusCts?.Dispose();
            statusTimer = null;
            statusCts = null;
            statusTask = null;
        }
    }

    private async Task RunStatusReportingLoopAsync(CancellationToken cancellationToken)
    {
        while (await statusTimer!.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                var currentState = useGrpc
                    ? grpcConnectionManager!.State
                    : signalRConnectionManager!.State;

                if (currentState != ConnectionState.Connected)
                {
                    continue;
                }

                var status = await statusProvider!.GetCurrentStatusAsync(cancellationToken).ConfigureAwait(false);

                if (useGrpc)
                {
                    await grpcConnectionManager!.ReportStatusAsync(status, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var hub = signalRConnectionManager!.HubConnection;
                    if (hub is null)
                    {
                        continue;
                    }
                    await hub.InvokeAsync(HubConstants.ClientMethods.ReportStatus, status, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send status report");
            }
        }
    }

    private static HttpClient CreateDefaultHttpClient(DeviceManagerClientOptions options)
    {
        return new HttpClient
        {
            BaseAddress = new Uri(options.ServerUrl.TrimEnd('/') + "/"),
            Timeout = options.ApiTimeout
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;

        await StopStatusReportingAsync().ConfigureAwait(false);
        messageClient.Dispose();
        configManager.Dispose();

        if (grpcConnectionManager is not null)
        {
            await grpcConnectionManager.DisposeAsync().ConfigureAwait(false);
        }

        if (signalRConnectionManager is not null)
        {
            await signalRConnectionManager.DisposeAsync().ConfigureAwait(false);
        }
    }
}
