using DeviceManager.Server.Core.Services;
using DeviceManager.Server.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManager.Server.Web.Controllers;

[ApiController]
[Route("api/datastore")]
public sealed class DataStoreController : ControllerBase
{
    private readonly DataStoreService _dataStoreService;

    public DataStoreController(DataStoreService dataStoreService)
    {
        _dataStoreService = dataStoreService;
    }

    [HttpGet("common")]
    public async Task<IActionResult> GetCommonEntries()
    {
        var entries = await _dataStoreService.GetCommonEntriesAsync();
        return Ok(entries);
    }

    [HttpGet("common/{key}")]
    public async Task<IActionResult> GetCommonEntry(string key)
    {
        var entry = await _dataStoreService.GetCommonEntryAsync(key);
        if (entry is null)
            return NotFound();

        return Ok(entry);
    }

    [HttpPut("common/{key}")]
    public async Task<IActionResult> SetCommonEntry(string key, [FromBody] DataStoreValueRequest request)
    {
        await _dataStoreService.SetCommonEntryAsync(key, request.Value);
        return NoContent();
    }

    [HttpDelete("common/{key}")]
    public async Task<IActionResult> DeleteCommonEntry(string key)
    {
        await _dataStoreService.DeleteCommonEntryAsync(key);
        return NoContent();
    }

    [HttpGet("devices/{deviceId}")]
    public async Task<IActionResult> GetDeviceEntries(string deviceId)
    {
        var entries = await _dataStoreService.GetDeviceEntriesAsync(deviceId);
        return Ok(entries);
    }

    [HttpGet("devices/{deviceId}/{key}")]
    public async Task<IActionResult> GetDeviceEntry(string deviceId, string key)
    {
        var entry = await _dataStoreService.GetDeviceEntryAsync(deviceId, key);
        if (entry is null)
            return NotFound();

        return Ok(entry);
    }

    [HttpPut("devices/{deviceId}/{key}")]
    public async Task<IActionResult> SetDeviceEntry(string deviceId, string key, [FromBody] DataStoreValueRequest request)
    {
        await _dataStoreService.SetDeviceEntryAsync(deviceId, key, request.Value);
        return NoContent();
    }

    [HttpDelete("devices/{deviceId}/{key}")]
    public async Task<IActionResult> DeleteDeviceEntry(string deviceId, string key)
    {
        await _dataStoreService.DeleteDeviceEntryAsync(deviceId, key);
        return NoContent();
    }
}
