namespace DeviceManager.Server.Endpoints;

using DeviceManager.Server.Application;

public static class FunctionEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapFunctionEndpoints(this WebApplication app)
    {
        // 端末開発・検証用のモック API(サーバー上で編集した JSON を返す)
        var group = app.MapGroup(ApiRoutes.Function)
            .RequireAuthorization(Policies.DeviceOrUser);

        group.MapGet("/{name}", HandleGetAsync);
        group.MapPost("/{name}", HandlePostAsync);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    private static async ValueTask<IResult> HandleGetAsync(
        FunctionService functionService,
        string name)
    {
        var json = await functionService.InvokeAsync(name);
        return json is not null
            ? Results.Content(json, "application/json")
            : TypedResults.NotFound();
    }

    // 受信ボディをそのままエコー返却する(端末からの送信テスト用)
    private static async ValueTask<IResult> HandlePostAsync(
        FunctionService functionService,
        HttpContext context,
        string name,
        CancellationToken cancellationToken)
    {
        var function = await functionService.QueryAsync(name);
        if (function is null)
        {
            return TypedResults.NotFound();
        }

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);
        return Results.Content(body, "application/json");
    }
}
