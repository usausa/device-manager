namespace DeviceManager.Server.Web.Api.Web;

using DeviceManager.Server.Web.Application.Filters;

[Area("api")]
[Microsoft.AspNetCore.Mvc.Route("[area]/[controller]/[action]")]
[ApiController]
[HttpExceptionFilter]
public abstract class BaseApiController : ControllerBase
{
}
