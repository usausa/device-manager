namespace DeviceManager.Client.Sdk.Connection;

using System.Text.Json;

using DeviceManager.Shared.Grpc;
using DeviceManager.Shared.Models;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Logging;

internal sealed class GrpcConnectionManager : IAsyncDisposable
{
    private readonly DeviceManagerClientOptions options;
    private readonly IDeviceInfoProvider deviceInfo;
    private readonly IDeviceCommandHandler? commandHandler;
    private readonly ILogger logger;

    private GrpcChannel? grpcChannel;
    private DeviceManagerService.DeviceManagerServiceClient? client;
    private CancellationTokenSource? subscribeCts;
    private Task? subscribeTask;
    private ConnectionState state = ConnectionState.Disconnected;

    public event EventHandler<ConnectionState>? ConnectionStateChanged;
    public event EventHandler<(string Type, string Content)>? MessageReceived;
    public event EventHandler<ConfigEntry>? ConfigUpdated;

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

    internal DeviceManagerService.DeviceManagerServiceClient? Client => client;

    public GrpcConnectionManager(
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
        if (grpcChannel is not null)
        {
            await DisconnectAsync().ConfigureAwait(false);
        }

        State = ConnectionState.Connecting;

        try
        {
            var grpcUrl = options.GrpcUrl ?? options.ServerUrl;
            grpcChannel = GrpcChannel.ForAddress(grpcUrl);
            client = new DeviceManagerService.DeviceManagerServiceClient(grpcChannel);

            var registerRequest = new RegisterRequest
            {
                DeviceId = deviceInfo.DeviceId,
                Name = deviceInfo.DeviceName,
                Platform = deviceInfo.Platform ?? string.Empty,
            };

            if (deviceInfo.AdditionalInfo is not null)
            {
                foreach (var kvp in deviceInfo.AdditionalInfo)
                {
                    registerRequest.AdditionalInfo[kvp.Key] = kvp.Value;
                }
            }

            var response = await client.RegisterAsync(registerRequest, cancellationToken: cancellationToken);
            if (!response.Success)
            {
                State = ConnectionState.Disconnected;
                throw new InvalidOperationException($"Device registration failed: {response.Message}");
            }

            State = ConnectionState.Connected;
            logger.LogInformation("Connected to DeviceManager via gRPC as device {DeviceId}", deviceInfo.DeviceId);

            subscribeCts = new CancellationTokenSource();
            subscribeTask = RunSubscriptionLoopAsync(subscribeCts.Token);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            State = ConnectionState.Disconnected;
            logger.LogError(ex, "Failed to connect to DeviceManager via gRPC");
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        subscribeCts?.Cancel();

        if (subscribeTask is not null)
        {
            try { await subscribeTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }

        subscribeCts?.Dispose();
        subscribeCts = null;
        subscribeTask = null;

        if (grpcChannel is not null)
        {
            grpcChannel.Dispose();
            grpcChannel = null;
        }

        client = null;
        State = ConnectionState.Disconnected;
    }

    public async Task ReportStatusAsync(DeviceStatusReport report, CancellationToken cancellationToken = default)
    {
        if (client is null)
        {
            throw new InvalidOperationException("Not connected to the server.");
        }

        var statusReport = new StatusReport
        {
            DeviceId = deviceInfo.DeviceId,
            Level = report.Level,
            Progress = report.Progress
        };

        if (report.Battery.HasValue)
        {
            statusReport.Battery = report.Battery.Value;
        }

        if (report.Latitude.HasValue)
        {
            statusReport.Latitude = report.Latitude.Value;
        }

        if (report.Longitude.HasValue)
        {
            statusReport.Longitude = report.Longitude.Value;
        }

        if (report.WifiRssi.HasValue)
        {
            statusReport.WifiRssi = report.WifiRssi.Value;
        }

        if (report.CustomData is not null)
        {
            foreach (var kvp in report.CustomData)
            {
                statusReport.CustomData[kvp.Key] = kvp.Value;
            }
        }

        await client.ReportStatusAsync(statusReport, cancellationToken: cancellationToken);
    }

    public async Task SendMessageAsync(string messageType, string content, CancellationToken cancellationToken = default)
    {
        if (client is null)
        {
            throw new InvalidOperationException("Not connected to the server.");
        }

        await client.SendMessageAsync(
            new DeviceMessage { DeviceId = deviceInfo.DeviceId, MessageType = messageType, Content = content },
            cancellationToken: cancellationToken);
    }

    private async Task RunSubscriptionLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var subscribeRequest = new SubscribeRequest { DeviceId = deviceInfo.DeviceId };
                using var stream = client!.Subscribe(subscribeRequest, cancellationToken: cancellationToken);

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
                logger.LogWarning(ex, "gRPC subscription stream disconnected, reconnecting...");
                State = ConnectionState.Reconnecting;

                if (options.AutoReconnect && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);

                    try
                    {
                        var registerRequest = new RegisterRequest
                        {
                            DeviceId = deviceInfo.DeviceId,
                            Name = deviceInfo.DeviceName,
                            Platform = deviceInfo.Platform ?? string.Empty,
                        };
                        await client!.RegisterAsync(registerRequest, cancellationToken: cancellationToken);
                        State = ConnectionState.Connected;
                    }
                    catch (Exception reEx)
                    {
                        logger.LogError(reEx, "Failed to re-register during reconnection");
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
                    logger.LogWarning(ex, "Failed to deserialize ReceiveMessage payload");
                }
                break;

            case "ReceiveCommand":
                if (commandHandler is not null)
                {
                    try
                    {
                        var cmdData = JsonSerializer.Deserialize<CommandPayload>(serverEvent.Payload);
                        if (cmdData is not null)
                        {
                            var result = await commandHandler.HandleCommandAsync(cmdData.Command, cmdData.Payload).ConfigureAwait(false);
                            logger.LogDebug("Command {CommandId} handled: Success={Success}", cmdData.CommandId, result.Success);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error handling command from gRPC stream");
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
                    logger.LogWarning(ex, "Failed to deserialize ConfigUpdated payload");
                }
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
