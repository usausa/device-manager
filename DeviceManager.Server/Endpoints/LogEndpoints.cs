namespace DeviceManager.Server.Endpoints;

using DeviceManager.Server.Application;
using DeviceManager.Server.Infrastructure.Notifications;

public static class LogEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapLogEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Logs);

        // 端末用(API キー)。SDK のタンキング付きログ転送の受け口
        group.MapPost("/batch", HandleBatchAsync).RequireAuthorization(Policies.Device);

        // 管理用(Cookie)
        group.MapGet("/", HandleQueryAsync).RequireAuthorization();
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleBatchAsync(
        LogService logService,
        DeviceEventBus eventBus,
        LogUploadRequest request)
    {
        await logService.AddRangeAsync(request.DeviceId, request.Records);
        eventBus.Publish(DeviceEventType.LogReceived, request.DeviceId);
        return TypedResults.NoContent();
    }

    private static async ValueTask<IResult> HandleQueryAsync(
        LogService logService,
        string? deviceId,
        [Range(0, 5)] int minLevel = 0,
        [Range(1, 1000)] int take = 100)
    {
        var logs = await logService.QueryLatestAsync(deviceId, (DeviceLogLevel)minLevel, take);
        return TypedResults.Ok(logs);
    }
}
