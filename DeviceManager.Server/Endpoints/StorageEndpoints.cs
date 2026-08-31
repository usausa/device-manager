namespace DeviceManager.Server.Endpoints;

using DeviceManager.Infrastructure.Storage;
using DeviceManager.Server.Application;
using DeviceManager.Server.Infrastructure.Filters;

public static class StorageEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapStorageEndpoints(this WebApplication app)
    {
        // 端末(API キー)と管理ユーザー(Cookie)の両方から利用可能
        var group = app.MapGroup(ApiRoutes.Storage)
            .RequireAuthorization(Policies.DeviceOrUser)
            .AddEndpointFilter<StorageExceptionFilter>();

        group.MapGet("/{**path}", HandleGetAsync);
        group.MapPost("/{**path}", HandleUploadAsync);
        group.MapDelete("/{**path}", HandleDeleteAsync);
        group.MapPut("/mkdir/{**path}", HandleCreateDirectoryAsync);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    // パスが空または末尾スラッシュ、あるいはディレクトリの場合は一覧、それ以外はダウンロード
    private static async ValueTask<IResult> HandleGetAsync(
        IStorage storage,
        string? path,
        CancellationToken cancellationToken)
    {
        path ??= string.Empty;

        if ((path.Length == 0) || path.EndsWith('/') || await storage.DirectoryExistsAsync(path, cancellationToken))
        {
            var target = path.TrimEnd('/');
            if (!await storage.DirectoryExistsAsync(target, cancellationToken))
            {
                return TypedResults.NotFound();
            }

            var entries = await storage.ListEntriesAsync(target, cancellationToken);
            return TypedResults.Ok(new StorageListResponse { Entries = entries });
        }

        if (!await storage.FileExistsAsync(path, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var stream = await storage.ReadAsync(path, cancellationToken);
        return TypedResults.Stream(stream, "application/octet-stream", Path.GetFileName(path));
    }

    private static async ValueTask<IResult> HandleUploadAsync(
        IStorage storage,
        HttpContext context,
        string path,
        CancellationToken cancellationToken)
    {
        if (path.EndsWith('/'))
        {
            return TypedResults.BadRequest();
        }

        await storage.WriteAsync(path, context.Request.Body, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async ValueTask<IResult> HandleDeleteAsync(
        IStorage storage,
        string path,
        CancellationToken cancellationToken)
    {
        if (!await storage.FileExistsAsync(path, cancellationToken) &&
            !await storage.DirectoryExistsAsync(path, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        await storage.DeleteAsync(path, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async ValueTask<IResult> HandleCreateDirectoryAsync(
        IStorage storage,
        string path,
        CancellationToken cancellationToken)
    {
        await storage.CreateDirectoryAsync(path, cancellationToken);
        return TypedResults.NoContent();
    }
}
