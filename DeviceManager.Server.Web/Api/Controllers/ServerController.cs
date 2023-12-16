namespace DeviceManager.Server.Web.Api.Controllers;

using DeviceManager.Server.Web.Api;
using DeviceManager.Server.Web.Api.Models;

public sealed class ServerController : BaseApiController
{
    [HttpGet]
    public IActionResult Time()
    {
        return Ok(new ServerTimeResponse { DateTime = DateTime.Now });
    }
}
