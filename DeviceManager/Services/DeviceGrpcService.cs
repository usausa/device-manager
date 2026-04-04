namespace DeviceManager.Services;

using System.Globalization;
using System.Threading.Channels;

using global::DeviceManager.Shared.Grpc;

using Grpc.Core;

public sealed class DeviceGrpcService(
    DeviceService deviceService,
    ConfigService configService,
    DataStoreService dataStoreService,
    MessageService messageService,
    LogService logService,
    DeviceEventService events,
    GrpcEventDispatcher eventDispatcher,
    ILogger<DeviceGrpcService> logger) : DeviceManagerService.DeviceManagerServiceBase
{
    public override async Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        try
        {
            var registration = new DeviceRegistration
            {
                DeviceId = request.DeviceId,
                Name = request.Name,
                Platform = string.IsNullOrEmpty(request.Platform) ? null : request.Platform,
                Group = string.IsNullOrEmpty(request.Group) ? null : request.Group,
                AdditionalInfo = request.AdditionalInfo.Count > 0
                    ? new Dictionary<string, string>(request.AdditionalInfo)
                    : null
            };

            await deviceService.RegisterDeviceAsync(registration);
            await deviceService.UpdateConnectionStatusAsync(request.DeviceId, DeviceConnectionStatus.Active);
            await events.NotifyDeviceConnectedAsync(registration);

            logger.LogInformation("Device registered via gRPC: {DeviceId} ({Name})", request.DeviceId, request.Name);
            return new RegisterResponse { Success = true, Message = "Registered successfully" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register device {DeviceId} via gRPC", request.DeviceId);
            return new RegisterResponse { Success = false, Message = ex.Message };
        }
    }

    public override async Task<StatusResponse> ReportStatus(StatusReport request, ServerCallContext context)
    {
        try
        {
            var report = new DeviceStatusReport
            {
                Level = request.Level,
                Progress = request.Progress,
                Battery = request.HasBattery ? request.Battery : null,
                WifiRssi = request.HasWifiRssi ? request.WifiRssi : null,
                Latitude = request.HasLatitude ? request.Latitude : null,
                Longitude = request.HasLongitude ? request.Longitude : null,
                CustomData = request.CustomData.Count > 0 ? new Dictionary<string, string>(request.CustomData) : null
            };

            await deviceService.UpdateStatusAsync(request.DeviceId, report);
            await events.NotifyStatusUpdatedAsync(request.DeviceId, report);
            return new StatusResponse { Success = true };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to report status for {DeviceId} via gRPC", request.DeviceId);
            return new StatusResponse { Success = false };
        }
    }

    public override async Task<MessageResponse> SendMessage(DeviceMessage request, ServerCallContext context)
    {
        try
        {
            var message = new ServerMessage
            {
                DeviceId = string.IsNullOrEmpty(request.DeviceId) ? null : request.DeviceId,
                Direction = MessageDirection.DeviceToServer,
                MessageType = request.MessageType,
                Content = request.Content,
                Status = MessageStatus.Delivered,
                CreatedAt = DateTime.UtcNow
            };

            await messageService.AddMessageAsync(message);
            await events.NotifyMessageReceivedAsync(message);
            return new MessageResponse { Success = true, MessageId = message.MessageId };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process message from {DeviceId} via gRPC", request.DeviceId);
            return new MessageResponse { Success = false };
        }
    }

    public override async Task<LogResponse> SendLog(LogRequest request, ServerCallContext context)
    {
        try
        {
            var entry = new LogEntry
            {
                DeviceId = request.DeviceId,
                Level = (Shared.Models.LogLevel)request.Level,
                Category = request.Category,
                Message = request.Message,
                Exception = string.IsNullOrEmpty(request.Exception) ? null : request.Exception,
                Timestamp = string.IsNullOrEmpty(request.Timestamp)
                    ? DateTime.UtcNow
                    : DateTime.Parse(request.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            };

            await logService.AddLogEntryAsync(entry);
            await events.NotifyLogReceivedAsync(entry);
            return new LogResponse { Success = true };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process log from {DeviceId} via gRPC", request.DeviceId);
            return new LogResponse { Success = false };
        }
    }

    public override async Task<ConfigResponse> GetConfig(ConfigRequest request, ServerCallContext context)
    {
        var entries = await configService.GetResolvedConfigAsync(request.DeviceId);
        var response = new ConfigResponse();
        foreach (var entry in entries)
        {
            response.Items.Add(new ConfigItem
            {
                Key = entry.Key,
                Value = entry.Value,
                ValueType = entry.ValueType,
                Description = entry.Description ?? string.Empty
            });
        }

        return response;
    }

    public override async Task<DataStoreResponse> GetDataStoreValue(DataStoreRequest request, ServerCallContext context)
    {
        var entry = await dataStoreService.GetDeviceEntryAsync(request.DeviceId, request.Key);
        return entry is null
            ? new DataStoreResponse { Success = false, Value = string.Empty }
            : new DataStoreResponse { Success = true, Value = entry.Value };
    }

    public override async Task<DataStoreResponse> SetDataStoreValue(DataStoreSetRequest request, ServerCallContext context)
    {
        try
        {
            await dataStoreService.SetDeviceEntryAsync(request.DeviceId, request.Key, request.Value);
            return new DataStoreResponse { Success = true, Value = request.Value };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set data store for {DeviceId}, key {Key}", request.DeviceId, request.Key);
            return new DataStoreResponse { Success = false, Value = string.Empty };
        }
    }

    public override async Task Subscribe(
        SubscribeRequest request,
        IServerStreamWriter<ServerEvent> responseStream,
        ServerCallContext context)
    {
        var channel = Channel.CreateUnbounded<ServerEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        eventDispatcher.RegisterSubscriber(request.DeviceId, channel);
        logger.LogInformation("Device {DeviceId} subscribed to gRPC event stream", request.DeviceId);

        try
        {
            await foreach (var serverEvent in channel.Reader.ReadAllAsync(context.CancellationToken))
            {
                await responseStream.WriteAsync(serverEvent, context.CancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
        finally
        {
            eventDispatcher.UnregisterSubscriber(request.DeviceId, channel);
            channel.Writer.TryComplete();

            await deviceService.UpdateConnectionStatusAsync(request.DeviceId, DeviceConnectionStatus.Inactive);
            await events.NotifyDeviceDisconnectedAsync(request.DeviceId);
            logger.LogInformation("Device {DeviceId} unsubscribed from gRPC event stream", request.DeviceId);
        }
    }
}
