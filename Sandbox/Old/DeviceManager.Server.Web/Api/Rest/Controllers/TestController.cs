namespace DeviceManager.Server.Web.Api.Controllers;

using DeviceManager.Server.Web.Api.Models;

public sealed class TestController : BaseApiController
{
    [HttpGet]
    public IActionResult Time()
    {
        return Ok(new TestTimeResponse { DateTime = DateTime.Now });
    }

    [HttpGet]
    public IActionResult Error()
    {
        throw new InvalidOperationException("API error.");
    }
}
