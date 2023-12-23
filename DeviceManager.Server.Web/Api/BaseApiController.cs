namespace DeviceManager.Server.Web.Api;

using DeviceManager.Server.Web.Application.Filters;

[Area("api")]
[Microsoft.AspNetCore.Mvc.Route("[area]/[controller]/[action]")]
[ApiController]
[ApiExceptionFilter]
public abstract class BaseApiController : ControllerBase
{
}
