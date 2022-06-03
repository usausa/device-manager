namespace DeviceManager.Server.Web.Api;

using DeviceManager.Server.Web.Infrastructure.Filters;

[Area("api")]
[Route("[area]/[controller]/[action]")]
[ApiController]
[ApiExceptionFilter]
public class BaseApiController : ControllerBase
{
}
