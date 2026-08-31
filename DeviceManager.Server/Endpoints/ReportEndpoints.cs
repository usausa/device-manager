namespace DeviceManager.Server.Endpoints;

using DeviceManager.Server.Application;
using DeviceManager.Server.Infrastructure.Notifications;

public static class ReportEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapReportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Reports);

        // 端末用(API キー)
        group.MapPost("/", HandleCreateAsync).RequireAuthorization(Policies.Device);

        // 管理用(Cookie)
        group.MapGet("/", HandleListAsync).RequireAuthorization();
        group.MapGet("/{id:long}", HandleGetAsync).RequireAuthorization();
        group.MapDelete("/{id:long}", HandleDeleteAsync).RequireAuthorization(Policies.Administrator);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleCreateAsync(
        ErrorReportService errorReportService,
        DeviceEventBus eventBus,
        ErrorReportRequest request)
    {
        var id = await errorReportService.AddAsync(request);
        eventBus.Publish(DeviceEventType.ErrorReportReceived, request.DeviceId);
        return TypedResults.Created(new Uri($"{ApiRoutes.Reports}/{id}", UriKind.Relative));
    }

    private static async ValueTask<IResult> HandleListAsync(
        ErrorReportUsecase errorReportUsecase,
        string? deviceId,
        [Range(0, Int32.MaxValue)] int page = 0,
        [Range(1, 100)] int size = 20)
    {
        var result = await errorReportUsecase.QueryPageAsync(deviceId, page, size);
        return TypedResults.Ok(result);
    }

    private static async ValueTask<IResult> HandleGetAsync(
        ErrorReportService errorReportService,
        long id)
    {
        var entity = await errorReportService.QueryAsync(id);
        return entity is not null ? TypedResults.Ok(entity) : TypedResults.NotFound();
    }

    private static async ValueTask<IResult> HandleDeleteAsync(
        ErrorReportService errorReportService,
        long id)
    {
        var deleted = await errorReportService.DeleteAsync(id);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
