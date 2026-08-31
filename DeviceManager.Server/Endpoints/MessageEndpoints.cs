namespace DeviceManager.Server.Endpoints;

using DeviceManager.Server.Application;
using DeviceManager.Server.Infrastructure.Devices;
using DeviceManager.Server.Models.Message;

public static class MessageEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapMessageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Messages)
            .RequireAuthorization();

        group.MapGet("/", HandleListAsync);
        group.MapPost("/send", HandleSendAsync);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleListAsync(
        MessageUsecase messageUsecase,
        string? deviceId,
        [Range(0, Int32.MaxValue)] int page = 0,
        [Range(1, 100)] int size = 20)
    {
        var result = await messageUsecase.QueryPageAsync(deviceId, page, size);
        return TypedResults.Ok(result);
    }

    private static async ValueTask<IResult> HandleSendAsync(
        DeviceMessenger messenger,
        MessageSendRequest request,
        CancellationToken cancellationToken)
    {
        var status = await messenger.SendMessageAsync(
            String.IsNullOrEmpty(request.DeviceId) ? null : request.DeviceId,
            request.MessageType,
            request.Content,
            cancellationToken);
        return TypedResults.Ok(new MessageSendResponse(status));
    }
}
