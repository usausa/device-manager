using DeviceManager.Shared;
using DeviceManager.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace DeviceManager.Client.Sdk.Connection;

internal sealed class SignalRConnectionManager : IAsyncDisposable
{
    private readonly DeviceManagerClientOptions _options;
    private readonly IDeviceInfoProvider _deviceInfo;
    private readonly IDeviceCommandHandler? _commandHandler;
    private readonly ILogger _logger;

    private HubConnection? _hubConnection;
    private ConnectionState _state = ConnectionState.Disconnected;

    public event EventHandler<ConnectionState>? ConnectionStateChanged;

    public ConnectionState State
    {
        get => _state;
        private set
        {
            if (_state == value) return;
            _state = value;
            ConnectionStateChanged?.Invoke(this, value);
        }
    }

    internal HubConnection? HubConnection => _hubConnection;

    public SignalRConnectionManager(
        DeviceManagerClientOptions options,
        IDeviceInfoProvider deviceInfo,
        IDeviceCommandHandler? commandHandler,
        ILogger logger)
    {
        _options = options;
        _deviceInfo = deviceInfo;
        _commandHandler = commandHandler;
        _logger = logger;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection is not null)
        {
            await DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }

        State = ConnectionState.Connecting;

        var builder = new HubConnectionBuilder()
            .WithUrl(new Uri(new Uri(_options.ServerUrl), HubConstants.DeviceHubPath).ToString());

        if (_options.AutoReconnect)
        {
            builder.WithAutomaticReconnect(new ExponentialBackoffRetryPolicy(_options.MaxReconnectInterval));
        }

        _hubConnection = builder.Build();

        _hubConnection.Closed += OnClosed;
        _hubConnection.Reconnecting += OnReconnecting;
        _hubConnection.Reconnected += OnReconnected;

        RegisterCommandHandler();

        try
        {
            await _hubConnection.StartAsync(cancellationToken).ConfigureAwait(false);
            await RegisterDeviceAsync(cancellationToken).ConfigureAwait(false);
            State = ConnectionState.Connected;
            _logger.LogInformation("Connected to DeviceManager hub as device {DeviceId}", _deviceInfo.DeviceId);
        }
        catch (Exception ex)
        {
            State = ConnectionState.Disconnected;
            _logger.LogError(ex, "Failed to connect to DeviceManager hub");
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection is null) return;

        try
        {
            await _hubConnection.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while disconnecting from hub");
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
            DeviceId = _deviceInfo.DeviceId,
            Name = _deviceInfo.DeviceName,
            Platform = _deviceInfo.Platform,
            AdditionalInfo = _deviceInfo.AdditionalInfo
        };

        await _hubConnection!.InvokeAsync(
            HubConstants.ClientMethods.Register,
            registration,
            cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Device registered: {DeviceId}", _deviceInfo.DeviceId);
    }

    private void RegisterCommandHandler()
    {
        if (_commandHandler is null || _hubConnection is null) return;

        _hubConnection.On<string, string, string>(
            HubConstants.ServerMethods.ReceiveCommand,
            async (commandId, command, payload) =>
            {
                _logger.LogDebug("Received command {CommandId}: {Command}", commandId, command);
                try
                {
                    var result = await _commandHandler.HandleCommandAsync(command, payload).ConfigureAwait(false);
                    await _hubConnection.InvokeAsync(
                        HubConstants.ClientMethods.CommandResult,
                        new CommandResult { CommandId = commandId, Success = result.Success, Result = result.Result })
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling command {CommandId}", commandId);
                    await _hubConnection.InvokeAsync(
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
            _logger.LogWarning(exception, "Hub connection closed with error");
        }

        State = ConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    private Task OnReconnecting(Exception? exception)
    {
        _logger.LogInformation(exception, "Reconnecting to hub...");
        State = ConnectionState.Reconnecting;
        return Task.CompletedTask;
    }

    private async Task OnReconnected(string? connectionId)
    {
        _logger.LogInformation("Reconnected to hub with connection ID {ConnectionId}", connectionId);
        try
        {
            await RegisterDeviceAsync(CancellationToken.None).ConfigureAwait(false);
            State = ConnectionState.Connected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to re-register device after reconnection");
        }
    }

    private async Task DisposeHubConnectionAsync()
    {
        if (_hubConnection is not null)
        {
            _hubConnection.Closed -= OnClosed;
            _hubConnection.Reconnecting -= OnReconnecting;
            _hubConnection.Reconnected -= OnReconnected;
            await _hubConnection.DisposeAsync().ConfigureAwait(false);
            _hubConnection = null;
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
