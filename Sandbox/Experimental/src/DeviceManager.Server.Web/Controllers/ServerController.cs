using Microsoft.AspNetCore.Mvc;

namespace DeviceManager.Server.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ServerController : ControllerBase
{
    [HttpGet("time")]
    public IActionResult GetTime()
    {
        return Ok(new { DateTime = DateTime.UtcNow });
    }
}
