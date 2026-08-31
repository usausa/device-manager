namespace DeviceManager.Server.Endpoints;

using DeviceManager.Server.Application;

public static class ConfigEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapConfigEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.Config);

        // 端末用(API キー)。解決済み(共通 + 端末別上書き)のコンフィグを取得する
        group.MapGet("/devices/{deviceId}/resolved", HandleResolvedAsync).RequireAuthorization(Policies.DeviceOrUser);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleResolvedAsync(
        ConfigService configService,
        string deviceId)
    {
        var values = await configService.QueryResolvedAsync(deviceId);
        return TypedResults.Ok(values);
    }
}
