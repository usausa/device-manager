namespace DeviceManager.Server.Web.Application.Filters;

using Microsoft.AspNetCore.Mvc.Filters;

// TODO delete
public sealed class ApiExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        context.Result = new ObjectResult(new
        {
            Code = 500,
            Message = "A server error occurred."
        })
        {
            StatusCode = 500
        };
    }
}
