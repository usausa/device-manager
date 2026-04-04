namespace DeviceManager.Client.Sdk.Connection;

using DeviceManager.Shared;
using DeviceManager.Shared.Models;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

internal sealed class SignalRConnectionManager : IAsyncDisposable
{
    private readonly DeviceManagerClientOptions options;
    private readonly IDeviceInfoProvider deviceInfo;
    private readonly IDeviceCommandHandler? commandHandler;
    private readonly ILogger logger;

    private HubConnection? hubConnection;
    private ConnectionState state = ConnectionState.Disconnected;

    public event EventHandler<ConnectionState>? ConnectionStateChanged;

    public ConnectionState State
    {
        get => state;
        private set
        {
            if (state == value)
            {
                return;
            }
            state = value;
            ConnectionStateChanged?.Invoke(this, value);
        }
    }

    internal HubConnection? HubConnection => hubConnection;

    public SignalRConnectionManager(
        DeviceManagerClientOptions options,
        IDeviceInfoProvider deviceInfo,
        IDeviceCommandHandler? commandHandler,
        ILogger logger)
    {
        this.options = options;
        this.deviceInfo = deviceInfo;
        this.commandHandler = commandHandler;
        this.logger = logger;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (hubConnection is not null)
        {
            await DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }

        State = ConnectionState.Connecting;

        var builder = new HubConnectionBuilder()
            .WithUrl(new Uri(new Uri(options.ServerUrl), HubConstants.DeviceHubPath).ToString());

        if (options.AutoReconnect)
        {
            builder.WithAutomaticReconnect(new ExponentialBackoffRetryPolicy(options.MaxReconnectInterval));
        }

        hubConnection = builder.Build();

        hubConnection.Closed += OnClosed;
        hubConnection.Reconnecting += OnReconnecting;
        hubConnection.Reconnected += OnReconnected;

        RegisterCommandHandler();

        try
        {
            await hubConnection.StartAsync(cancellationToken).ConfigureAwait(false);
            await RegisterDeviceAsync(cancellationToken).ConfigureAwait(false);
            State = ConnectionState.Connected;
            logger.LogInformation("Connected to DeviceManager hub as device {DeviceId}", deviceInfo.DeviceId);
        }
        catch (Exception ex)
        {
            State = ConnectionState.Disconnected;
            logger.LogError(ex, "Failed to connect to DeviceManager hub");
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (hubConnection is null)
        {
            return;
        }

        try
        {
            await hubConnection.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error while disconnecting from hub");
        }
        finally
        {
            await DisposeHubConnectionAsync().ConfigureAwait(false);
            State = ConnectionState.Disconnected;
        }
    }

    private async Task RegisterDeviceAsync(CancellationToken cancellationToken)
    {
        var registration = new DeviceRegistration
        {
            DeviceId = deviceInfo.DeviceId,
            Name = deviceInfo.DeviceName,
            Platform = deviceInfo.Platform,
            AdditionalInfo = deviceInfo.AdditionalInfo
        };

        await hubConnection!.InvokeAsync(
            HubConstants.ClientMethods.Register,
            registration,
            cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Device registered: {DeviceId}", deviceInfo.DeviceId);
    }

    private void RegisterCommandHandler()
    {
        if (commandHandler is null || hubConnection is null)
        {
            return;
        }

        hubConnection.On<string, string, string>(
            HubConstants.ServerMethods.ReceiveCommand,
            async (commandId, command, payload) =>
            {
                logger.LogDebug("Received command {CommandId}: {Command}", commandId, command);
                try
                {
                    var result = await commandHandler.HandleCommandAsync(command, payload).ConfigureAwait(false);
                    await hubConnection.InvokeAsync(
                        HubConstants.ClientMethods.CommandResult,
                        new CommandResult { CommandId = commandId, Success = result.Success, Result = result.Result })
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error handling command {CommandId}", commandId);
                    await hubConnection.InvokeAsync(
                        HubConstants.ClientMethods.CommandResult,
                        new CommandResult { CommandId = commandId, Success = false, Result = ex.Message })
                        .ConfigureAwait(false);
                }
            });
    }

    private Task OnClosed(Exception? exception)
    {
        if (exception is not null)
        {
            logger.LogWarning(exception, "Hub connection closed with error");
        }

        State = ConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    private Task OnReconnecting(Exception? exception)
    {
        logger.LogInformation(exception, "Reconnecting to hub...");
        State = ConnectionState.Reconnecting;
        return Task.CompletedTask;
    }

    private async Task OnReconnected(string? connectionId)
    {
        logger.LogInformation("Reconnected to hub with connection ID {ConnectionId}", connectionId);
        try
        {
            await RegisterDeviceAsync(CancellationToken.None).ConfigureAwait(false);
            State = ConnectionState.Connected;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to re-register device after reconnection");
        }
    }

    private async Task DisposeHubConnectionAsync()
    {
        if (hubConnection is not null)
        {
            hubConnection.Closed -= OnClosed;
            hubConnection.Reconnecting -= OnReconnecting;
            hubConnection.Reconnected -= OnReconnected;
            await hubConnection.DisposeAsync().ConfigureAwait(false);
            hubConnection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
    }

    private sealed class ExponentialBackoffRetryPolicy(TimeSpan maxInterval) : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            var delay = TimeSpan.FromSeconds(Math.Pow(2, retryContext.PreviousRetryCount));
            return delay < maxInterval ? delay : maxInterval;
        }
    }
}
