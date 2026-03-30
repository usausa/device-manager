namespace DeviceMonitor.Server.Web.Api;

using DeviceMonitor.Server.Web.Infrastructure.Filters;

[Area("api")]
[Route("[area]/[controller]/[action]")]
[ApiController]
[ApiExceptionFilter]
public class BaseApiController : ControllerBase
{
}
