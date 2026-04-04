namespace DeviceManager.Controllers;

using DeviceManager.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

[ApiController]
[Route("api/[controller]")]
public sealed class DevicesController(DeviceService deviceService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await deviceService.GetAllDevicesAsync());

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary() => Ok(await deviceService.GetStatusSummaryAsync());

    [HttpGet("{deviceId}")]
    public async Task<IActionResult> Get(string deviceId)
    {
        var device = await deviceService.GetDeviceAsync(deviceId);
        return device is null ? NotFound() : Ok(device);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DeviceRegistration registration)
    {
        await deviceService.RegisterDeviceAsync(registration);
        return CreatedAtAction(nameof(Get), new { deviceId = registration.DeviceId }, registration);
    }

    [HttpPut("{deviceId}")]
    public async Task<IActionResult> Update(string deviceId, [FromBody] DeviceUpdateRequest request)
    {
        var existing = await deviceService.GetDeviceAsync(deviceId);
        if (existing is null)
        {
            return NotFound();
        }

        await deviceService.UpdateDeviceAsync(deviceId, request);
        return NoContent();
    }

    [HttpDelete("{deviceId}")]
    public async Task<IActionResult> Delete(string deviceId)
    {
        var existing = await deviceService.GetDeviceAsync(deviceId);
        if (existing is null)
        {
            return NotFound();
        }

        await deviceService.DeleteDeviceAsync(deviceId);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public sealed class MessagesController(
    MessageService messageService,
    IHubContext<DeviceHub> hubContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMessages([FromQuery] string? deviceId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
        => Ok(await messageService.GetMessagesAsync(deviceId, skip, take));

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

        await messageService.AddMessageAsync(message);

        if (!string.IsNullOrEmpty(request.DeviceId))
        {
            await hubContext.Clients.Group(HubConstants.Groups.Device(request.DeviceId))
                .SendAsync(HubConstants.ServerMethods.ReceiveMessage, request.MessageType, request.Content);
        }
        else
        {
            await hubContext.Clients.Group(HubConstants.Groups.AllDevices)
                .SendAsync(HubConstants.ServerMethods.ReceiveMessage, request.MessageType, request.Content);
        }

        return Ok(message);
    }

    [HttpPost("command")]
    public async Task<IActionResult> SendCommand([FromBody] SendCommandRequest request)
    {
        var commandId = Guid.NewGuid().ToString("N");
        await hubContext.Clients.Group(HubConstants.Groups.Device(request.DeviceId))
            .SendAsync(HubConstants.ServerMethods.ReceiveCommand, commandId, request.Command, request.Payload);
        return Ok(new { CommandId = commandId });
    }
}

[ApiController]
[Route("api/[controller]")]
public sealed class StorageController(IConfiguration configuration, ILogger<StorageController> logger) : ControllerBase
{
    private readonly string rootPath = InitRootPath(configuration);

    [HttpGet("{**path}")]
    public IActionResult Get(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return BadRequest("Path is required.");
        }

        var fullPath = GetSafePath(path);
        if (fullPath is null)
        {
            return BadRequest("Invalid path.");
        }

        if (path.EndsWith('/') || Directory.Exists(fullPath))
        {
            if (!Directory.Exists(fullPath))
            {
                return NotFound();
            }

            var entries = new List<object>();
            foreach (var dir in Directory.GetDirectories(fullPath))
            {
                entries.Add(new { Name = Path.GetFileName(dir), Type = "directory" });
            }

            foreach (var file in Directory.GetFiles(fullPath))
            {
                var info = new FileInfo(file);
                entries.Add(new { info.Name, Type = "file", info.Length, LastModified = info.LastWriteTimeUtc });
            }

            return Ok(entries);
        }

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, GetContentType(fullPath), Path.GetFileName(fullPath));
    }

    [HttpPost("{**path}")]
    public async Task<IActionResult> Upload(string path, IFormFile file)
    {
        if (string.IsNullOrEmpty(path))
        {
            return BadRequest("Path is required.");
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest("File is required.");
        }

        var fullPath = GetSafePath(path);
        if (fullPath is null)
        {
            return BadRequest("Invalid path.");
        }

        var directory = Path.GetDirectoryName(fullPath);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await file.CopyToAsync(stream);

        logger.LogInformation("File uploaded: {Path}", path);
        return CreatedAtAction(nameof(Get), new { path }, new { Path = path, Size = file.Length });
    }

    [HttpPut("mkdir/{**path}")]
    public IActionResult CreateDirectory(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return BadRequest("Path is required.");
        }

        var fullPath = GetSafePath(path);
        if (fullPath is null)
        {
            return BadRequest("Invalid path.");
        }

        Directory.CreateDirectory(fullPath);
        logger.LogInformation("Directory created: {Path}", path);
        return Ok();
    }

    [HttpDelete("{**path}")]
    public IActionResult Delete(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return BadRequest("Path is required.");
        }

        var fullPath = GetSafePath(path);
        if (fullPath is null)
        {
            return BadRequest("Invalid path.");
        }

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, recursive: true);
            logger.LogInformation("Directory deleted: {Path}", path);
            return NoContent();
        }

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        System.IO.File.Delete(fullPath);
        logger.LogInformation("File deleted: {Path}", path);
        return NoContent();
    }

    private static string InitRootPath(IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:RootPath"];
        var root = !string.IsNullOrEmpty(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.Combine(AppContext.BaseDirectory, "storage");
        Directory.CreateDirectory(root);
        return root;
    }

    private string? GetSafePath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        return !fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) ? null : fullPath;
    }

    private static string GetContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".txt" => "text/plain",
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".html" or ".htm" => "text/html",
        ".css" => "text/css",
        ".js" => "application/javascript",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".pdf" => "application/pdf",
        ".zip" => "application/zip",
        _ => "application/octet-stream"
    };
}
