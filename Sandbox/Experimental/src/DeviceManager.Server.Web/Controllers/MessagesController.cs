using DeviceManager.Server.Core.Services;
using DeviceManager.Server.Web.Hubs;
using DeviceManager.Shared;
using DeviceManager.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DeviceManager.Server.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MessagesController : ControllerBase
{
    private readonly MessageService _messageService;
    private readonly IHubContext<DeviceHub> _hubContext;

    public MessagesController(MessageService messageService, IHubContext<DeviceHub> hubContext)
    {
        _messageService = messageService;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetMessages([FromQuery] string? deviceId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var messages = await _messageService.GetMessagesAsync(deviceId, skip, take);
        return Ok(messages);
    }

    [HttpGet("devices/{deviceId}")]
    public async Task<IActionResult> GetDeviceMessages(string deviceId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var messages = await _messageService.GetMessagesAsync(deviceId, skip, take);
        return Ok(messages);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var message = new ServerMessage
        {
            DeviceId = request.DeviceId,
            Direction = MessageDirection.ServerToDevice,
            MessageType = request.MessageType,
            Content = request.Content,
            Status = MessageStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        await _messageService.AddMessageAsync(message);

        // Send via SignalR to the target device or all devices
        if (!string.IsNullOrEmpty(request.DeviceId))
        {
            await _hubContext.Clients.Group(HubConstants.Groups.Device(request.DeviceId))
                .SendAsync(HubConstants.ServerMethods.ReceiveMessage, request.MessageType, request.Content);
        }
        else
        {
            await _hubContext.Clients.Group(HubConstants.Groups.AllDevices)
                .SendAsync(HubConstants.ServerMethods.ReceiveMessage, request.MessageType, request.Content);
        }

        return Ok(message);
    }

    [HttpPost("command")]
    public async Task<IActionResult> SendCommand([FromBody] SendCommandRequest request)
    {
        var commandId = Guid.NewGuid().ToString("N");

        await _hubContext.Clients.Group(HubConstants.Groups.Device(request.DeviceId))
            .SendAsync(HubConstants.ServerMethods.ReceiveCommand, commandId, request.Command, request.Payload);

        return Ok(new { CommandId = commandId });
    }
}
