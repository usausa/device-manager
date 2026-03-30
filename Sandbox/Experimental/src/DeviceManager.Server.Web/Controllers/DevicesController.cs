using DeviceManager.Server.Core.Services;
using DeviceManager.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManager.Server.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class DevicesController : ControllerBase
{
    private readonly DeviceService _deviceService;

    public DevicesController(DeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var devices = await _deviceService.GetAllDevicesAsync();
        return Ok(devices);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _deviceService.GetStatusSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("{deviceId}")]
    public async Task<IActionResult> Get(string deviceId)
    {
        var device = await _deviceService.GetDeviceAsync(deviceId);
        if (device is null)
            return NotFound();

        return Ok(device);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DeviceRegistration registration)
    {
        await _deviceService.RegisterDeviceAsync(registration);
        return CreatedAtAction(nameof(Get), new { deviceId = registration.DeviceId }, registration);
    }

    [HttpPut("{deviceId}")]
    public async Task<IActionResult> Update(string deviceId, [FromBody] DeviceUpdateRequest request)
    {
        var existing = await _deviceService.GetDeviceAsync(deviceId);
        if (existing is null)
            return NotFound();

        await _deviceService.UpdateDeviceAsync(deviceId, request);
        return NoContent();
    }

    [HttpDelete("{deviceId}")]
    public async Task<IActionResult> Delete(string deviceId)
    {
        var existing = await _deviceService.GetDeviceAsync(deviceId);
        if (existing is null)
            return NotFound();

        await _deviceService.DeleteDeviceAsync(deviceId);
        return NoContent();
    }

    [HttpPost("{deviceId}/status")]
    public async Task<IActionResult> ReportStatus(string deviceId, [FromBody] DeviceStatusReport report)
    {
        var existing = await _deviceService.GetDeviceAsync(deviceId);
        if (existing is null)
            return NotFound();

        await _deviceService.UpdateStatusAsync(deviceId, report);
        return Ok();
    }

    [HttpGet("{deviceId}/status/history")]
    public async Task<IActionResult> GetStatusHistory(string deviceId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var history = await _deviceService.GetStatusHistoryAsync(deviceId, from, to);
        return Ok(history);
    }
}
