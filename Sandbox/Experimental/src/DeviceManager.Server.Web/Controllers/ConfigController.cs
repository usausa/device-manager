using DeviceManager.Server.Core.Services;
using DeviceManager.Server.Web.Hubs;
using DeviceManager.Shared;
using DeviceManager.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DeviceManager.Server.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ConfigController : ControllerBase
{
    private readonly ConfigService _configService;
    private readonly IHubContext<DeviceHub> _hubContext;

    public ConfigController(ConfigService configService, IHubContext<DeviceHub> hubContext)
    {
        _configService = configService;
        _hubContext = hubContext;
    }

    [HttpGet("common")]
    public async Task<IActionResult> GetCommonConfig()
    {
        var config = await _configService.GetCommonConfigAsync();
        return Ok(config);
    }

    [HttpPut("common/{key}")]
    public async Task<IActionResult> SetCommonConfig(string key, [FromBody] ConfigEntry entry)
    {
        await _configService.SetCommonConfigAsync(key, entry);

        // Notify all devices that config has been updated
        await _hubContext.Clients.Group(HubConstants.Groups.AllDevices)
            .SendAsync(HubConstants.ServerMethods.ConfigUpdated, key, entry);

        return NoContent();
    }

    [HttpDelete("common/{key}")]
    public async Task<IActionResult> DeleteCommonConfig(string key)
    {
        await _configService.DeleteCommonConfigAsync(key);
        return NoContent();
    }

    [HttpGet("devices/{deviceId}")]
    public async Task<IActionResult> GetDeviceConfig(string deviceId)
    {
        var config = await _configService.GetDeviceConfigAsync(deviceId);
        return Ok(config);
    }

    [HttpPut("devices/{deviceId}/{key}")]
    public async Task<IActionResult> SetDeviceConfig(string deviceId, string key, [FromBody] ConfigEntry entry)
    {
        await _configService.SetDeviceConfigAsync(deviceId, key, entry);

        // Notify the specific device that its config has been updated
        await _hubContext.Clients.Group(HubConstants.Groups.Device(deviceId))
            .SendAsync(HubConstants.ServerMethods.ConfigUpdated, key, entry);

        return NoContent();
    }

    [HttpDelete("devices/{deviceId}/{key}")]
    public async Task<IActionResult> DeleteDeviceConfig(string deviceId, string key)
    {
        await _configService.DeleteDeviceConfigAsync(deviceId, key);
        return NoContent();
    }

    [HttpGet("devices/{deviceId}/resolved")]
    public async Task<IActionResult> GetResolvedConfig(string deviceId)
    {
        var config = await _configService.GetResolvedConfigAsync(deviceId);
        return Ok(config);
    }
}
