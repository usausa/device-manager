using System.Text.Json;
using DeviceManager.Shared.Grpc;
using DeviceManager.Shared.Models;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace DeviceManager.Client.Sdk.Connection;

internal sealed class GrpcConnectionManager : IAsyncDisposable
{
    private readonly DeviceManagerClientOptions _options;
    private readonly IDeviceInfoProvider _deviceInfo;
    private readonly IDeviceCommandHandler? _commandHandler;
    private readonly ILogger _logger;

    private GrpcChannel? _grpcChannel;
    private DeviceManagerService.DeviceManagerServiceClient? _client;
    private CancellationTokenSource? _subscribeCts;
    private Task? _subscribeTask;
    private ConnectionState _state = ConnectionState.Disconnected;

    public event EventHandler<ConnectionState>? ConnectionStateChanged;
    public event EventHandler<(string Type, string Content)>? MessageReceived;
    public event EventHandler<ConfigEntry>? ConfigUpdated;

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

    internal DeviceManagerService.DeviceManagerServiceClient? Client => _client;

    public GrpcConnectionManager(
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
        if (_grpcChannel is not null)
        {
            await DisconnectAsync(cancellationToken).ConfigureAwait(false);
        }

        State = ConnectionState.Connecting;

        try
        {
            var grpcUrl = _options.GrpcUrl ?? _options.ServerUrl;
            _grpcChannel = GrpcChannel.ForAddress(grpcUrl);
            _client = new DeviceManagerService.DeviceManagerServiceClient(_grpcChannel);

            // Register the device
            var registerRequest = new RegisterRequest
            {
                DeviceId = _deviceInfo.DeviceId,
                Name = _deviceInfo.DeviceName,
                Platform = _deviceInfo.Platform ?? string.Empty,
            };

            if (_deviceInfo.AdditionalInfo is not null)
            {
                foreach (var kvp in _deviceInfo.AdditionalInfo)
                {
                    registerRequest.AdditionalInfo[kvp.Key] = kvp.Value;
                }
            }

            var response = await _client.RegisterAsync(registerRequest, cancellationToken: cancellationToken);
            if (!response.Success)
            {
                State = ConnectionState.Disconnected;
                throw new InvalidOperationException($"Device registration failed: {response.Message}");
            }

            State = ConnectionState.Connected;
            _logger.LogInformation("Connected to DeviceManager via gRPC as device {DeviceId}", _deviceInfo.DeviceId);

            // Start the subscription stream
            _subscribeCts = new CancellationTokenSource();
            _subscribeTask = RunSubscriptionLoopAsync(_subscribeCts.Token);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            State = ConnectionState.Disconnected;
            _logger.LogError(ex, "Failed to connect to DeviceManager via gRPC");
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _subscribeCts?.Cancel();

        if (_subscribeTask is not null)
        {
            try
            {
                await _subscribeTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _subscribeCts?.Dispose();
        _subscribeCts = null;
        _subscribeTask = null;

        if (_grpcChannel is not null)
        {
            _grpcChannel.Dispose();
            _grpcChannel = null;
        }

        _client = null;
        State = ConnectionState.Disconnected;
    }

    public async Task ReportStatusAsync(DeviceStatusReport report, CancellationToken cancellationToken = default)
    {
        if (_client is null)
            throw new InvalidOperationException("Not connected to the server.");

        var statusReport = new StatusReport
        {
            DeviceId = _deviceInfo.DeviceId,
            Level = report.Level,
            Progress = report.Progress
        };

        if (report.Battery.HasValue)
            statusReport.Battery = report.Battery.Value;
        if (report.Latitude.HasValue)
            statusReport.Latitude = report.Latitude.Value;
        if (report.Longitude.HasValue)
            statusReport.Longitude = report.Longitude.Value;

        if (report.CustomData is not null)
        {
            foreach (var kvp in report.CustomData)
            {
                statusReport.CustomData[kvp.Key] = kvp.Value;
            }
        }

        await _client.ReportStatusAsync(statusReport, cancellationToken: cancellationToken);
    }

    public async Task SendMessageAsync(string messageType, string content, CancellationToken cancellationToken = default)
    {
        if (_client is null)
            throw new InvalidOperationException("Not connected to the server.");

        var message = new DeviceMessage
        {
            DeviceId = _deviceInfo.DeviceId,
            MessageType = messageType,
            Content = content
        };

        await _client.SendMessageAsync(message, cancellationToken: cancellationToken);
    }

    private async Task RunSubscriptionLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var subscribeRequest = new SubscribeRequest { DeviceId = _deviceInfo.DeviceId };
                using var stream = _client!.Subscribe(subscribeRequest, cancellationToken: cancellationToken);

                await foreach (var serverEvent in stream.ResponseStream.ReadAllAsync(cancellationToken))
                {
                    await HandleServerEventAsync(serverEvent).ConfigureAwait(false);
                }
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Cancelled)
            {
                break;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "gRPC subscription stream disconnected, reconnecting...");
                State = ConnectionState.Reconnecting;

                if (_options.AutoReconnect && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);

                    try
                    {
                        var registerRequest = new RegisterRequest
                        {
                            DeviceId = _deviceInfo.DeviceId,
                            Name = _deviceInfo.DeviceName,
                            Platform = _deviceInfo.Platform ?? string.Empty,
                        };

                        await _client!.RegisterAsync(registerRequest, cancellationToken: cancellationToken);
                        State = ConnectionState.Connected;
                    }
                    catch (Exception reEx)
                    {
                        _logger.LogError(reEx, "Failed to re-register during reconnection");
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }

    private async Task HandleServerEventAsync(ServerEvent serverEvent)
    {
        switch (serverEvent.EventType)
        {
            case "ReceiveMessage":
                try
                {
                    var msgData = JsonSerializer.Deserialize<MessagePayload>(serverEvent.Payload);
                    if (msgData is not null)
                    {
                        MessageReceived?.Invoke(this, (msgData.MessageType, msgData.Content));
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize ReceiveMessage payload");
                }
                break;

            case "ReceiveCommand":
                if (_commandHandler is not null)
                {
                    try
                    {
                        var cmdData = JsonSerializer.Deserialize<CommandPayload>(serverEvent.Payload);
                        if (cmdData is not null)
                        {
                            var result = await _commandHandler.HandleCommandAsync(cmdData.Command, cmdData.Payload).ConfigureAwait(false);
                            _logger.LogDebug("Command {CommandId} handled: Success={Success}", cmdData.CommandId, result.Success);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling command from gRPC stream");
                    }
                }
                break;

            case "ConfigUpdated":
                try
                {
                    var configData = JsonSerializer.Deserialize<ConfigEntry>(serverEvent.Payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    if (configData is not null)
                    {
                        ConfigUpdated?.Invoke(this, configData);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize ConfigUpdated payload");
                }
                break;

            default:
                _logger.LogDebug("Received unknown event type: {EventType}", serverEvent.EventType);
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync().ConfigureAwait(false);
    }

    private sealed class MessagePayload
    {
        public string MessageType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private sealed class CommandPayload
    {
        public string CommandId { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string? Payload { get; set; }
    }
}
