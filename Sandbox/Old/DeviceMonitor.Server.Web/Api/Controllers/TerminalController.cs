namespace DeviceMonitor.Server.Web.Api.Controllers;

using DeviceMonitor.Server.Web.Api.Models;

public class TerminalController : BaseApiController
{
    [HttpPost]
    public IActionResult Status([FromBody] TerminalStatusRequest request)
    {
        // TODO
        return Ok(request);
    }

    [HttpGet]
    public IActionResult Time()
    {
        return Ok(new TerminalTimeResponse { DateTime = DateTime.Now });
    }
}
