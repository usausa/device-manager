namespace DeviceManager.Server.Endpoints;

using DeviceManager.Server.Application;
using DeviceManager.Server.Infrastructure.Notifications;

public static class DeviceEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapDeviceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Devices);

        // 管理用(Cookie)
        group.MapGet("/", HandleListAsync).RequireAuthorization();
        group.MapGet("/summary", HandleSummaryAsync).RequireAuthorization();

        // 端末用(API キー)。ハブを使わない REST のみの端末向け
        group.MapPost("/{deviceId}/status", HandleStatusAsync).RequireAuthorization(Policies.Device);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleListAsync(
        DeviceService deviceService,
        string? filter)
    {
        var devices = await deviceService.QueryListAsync(filter);
        return TypedResults.Ok(devices);
    }

    private static async ValueTask<IResult> HandleSummaryAsync(
        DeviceUsecase deviceUsecase,
        DeviceSetting setting)
    {
        var summary = await deviceUsecase.QuerySummaryAsync(setting.LowBatteryThreshold);
        return TypedResults.Ok(summary);
    }

    private static async ValueTask<IResult> HandleStatusAsync(
        DeviceService deviceService,
        DeviceEventBus eventBus,
        string deviceId,
        DeviceStatusReport report)
    {
        var device = await deviceService.QueryAsync(deviceId);
        if (device is null)
        {
            return TypedResults.NotFound();
        }

        await deviceService.ReportStatusAsync(deviceId, report);
        eventBus.Publish(DeviceEventType.StatusUpdated, deviceId);
        return TypedResults.NoContent();
    }
}
