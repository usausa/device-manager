namespace DeviceManager.Server.Web.Components;

#pragma warning disable CA1032
public abstract class AuthorizeException : Exception
{
    public int StatusCode { get; }

    protected AuthorizeException(int statusCode)
    {
        StatusCode = statusCode;
    }
}

public sealed class NotFoundException : AuthorizeException
{
    public NotFoundException()
        : base(StatusCodes.Status404NotFound)
    {
    }
}

public sealed class ForbiddenException : AuthorizeException
{
    public ForbiddenException()
        : base(StatusCodes.Status403Forbidden)
    {
    }
}
#pragma warning restore CA1032
