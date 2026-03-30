using System.Threading.Channels;
using DeviceManager.Server.Core.Services;
using DeviceManager.Server.Web.Hubs;
using DeviceManager.Shared;
using DeviceManager.Shared.Grpc;
using DeviceManager.Shared.Models;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;

namespace DeviceManager.Server.Web.Services;

public sealed class DeviceGrpcService : DeviceManagerService.DeviceManagerServiceBase
{
    private readonly DeviceService _deviceService;
    private readonly ConfigService _configService;
    private readonly DataStoreService _dataStoreService;
    private readonly MessageService _messageService;
    private readonly IHubContext<DeviceHub> _hubContext;
    private readonly GrpcEventDispatcher _eventDispatcher;
    private readonly ILogger<DeviceGrpcService> _logger;

    public DeviceGrpcService(
        DeviceService deviceService,
        ConfigService configService,
        DataStoreService dataStoreService,
        MessageService messageService,
        IHubContext<DeviceHub> hubContext,
        GrpcEventDispatcher eventDispatcher,
        ILogger<DeviceGrpcService> logger)
    {
        _deviceService = deviceService;
        _configService = configService;
        _dataStoreService = dataStoreService;
        _messageService = messageService;
        _hubContext = hubContext;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

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

            await _deviceService.RegisterDeviceAsync(registration);
            await _deviceService.UpdateConnectionStatusAsync(request.DeviceId, DeviceConnectionStatus.Active);

            // Notify dashboard via SignalR
            await _hubContext.Clients.Group(HubConstants.Groups.Dashboard)
                .SendAsync(HubConstants.DashboardMethods.DeviceConnected, registration);

            _logger.LogInformation("Device registered via gRPC: {DeviceId} ({Name})", request.DeviceId, request.Name);

            return new RegisterResponse { Success = true, Message = "Registered successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register device {DeviceId} via gRPC", request.DeviceId);
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
                Latitude = request.HasLatitude ? request.Latitude : null,
                Longitude = request.HasLongitude ? request.Longitude : null,
                CustomData = request.CustomData.Count > 0
                    ? new Dictionary<string, string>(request.CustomData)
                    : null
            };

            await _deviceService.UpdateStatusAsync(request.DeviceId, report);

            // Notify dashboard via SignalR
            await _hubContext.Clients.Group(HubConstants.Groups.Dashboard)
                .SendAsync(HubConstants.DashboardMethods.DeviceStatusUpdated, request.DeviceId, report);

            return new StatusResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report status for device {DeviceId} via gRPC", request.DeviceId);
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

            await _messageService.AddMessageAsync(message);

            // Notify dashboard via SignalR
            await _hubContext.Clients.Group(HubConstants.Groups.Dashboard)
                .SendAsync(HubConstants.DashboardMethods.MessageReceived, message);

            _logger.LogDebug("Message received via gRPC from {DeviceId}: {MessageType}", request.DeviceId, request.MessageType);

            return new MessageResponse { Success = true, MessageId = message.MessageId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message from device {DeviceId} via gRPC", request.DeviceId);
            return new MessageResponse { Success = false };
        }
    }

    public override async Task<ConfigResponse> GetConfig(ConfigRequest request, ServerCallContext context)
    {
        var entries = await _configService.GetResolvedConfigAsync(request.DeviceId);

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
        var entry = await _dataStoreService.GetDeviceEntryAsync(request.DeviceId, request.Key);
        if (entry is null)
        {
            return new DataStoreResponse { Success = false, Value = string.Empty };
        }

        return new DataStoreResponse { Success = true, Value = entry.Value };
    }

    public override async Task<DataStoreResponse> SetDataStoreValue(DataStoreSetRequest request, ServerCallContext context)
    {
        try
        {
            await _dataStoreService.SetDeviceEntryAsync(request.DeviceId, request.Key, request.Value);
            return new DataStoreResponse { Success = true, Value = request.Value };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set data store value for device {DeviceId}, key {Key}", request.DeviceId, request.Key);
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

        _eventDispatcher.RegisterSubscriber(request.DeviceId, channel);
        _logger.LogInformation("Device {DeviceId} subscribed to gRPC event stream", request.DeviceId);

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
            _eventDispatcher.UnregisterSubscriber(request.DeviceId, channel);
            channel.Writer.TryComplete();

            // Mark device as inactive
            await _deviceService.UpdateConnectionStatusAsync(request.DeviceId, DeviceConnectionStatus.Inactive);

            // Notify dashboard
            await _hubContext.Clients.Group(HubConstants.Groups.Dashboard)
                .SendAsync(HubConstants.DashboardMethods.DeviceDisconnected, request.DeviceId);

            _logger.LogInformation("Device {DeviceId} unsubscribed from gRPC event stream", request.DeviceId);
        }
    }
}
