namespace DeviceManager.Services;

using DeviceManager.Accessors;

public sealed class FunctionService
{
    private readonly FunctionAccessor functionAccessor;

    private readonly TimeProvider timeProvider;

    public FunctionService(
        FunctionAccessor functionAccessor,
        TimeProvider timeProvider)
    {
        this.functionAccessor = functionAccessor;
        this.timeProvider = timeProvider;
    }

    public void CreateTable() =>
        functionAccessor.Create();

    public ValueTask<List<FunctionEntity>> QueryAllAsync() =>
        functionAccessor.QueryAllAsync();

    public ValueTask<FunctionEntity?> QueryAsync(string name) =>
        functionAccessor.QueryAsync(name);

    public ValueTask<int> SetAsync(string name, string json) =>
        functionAccessor.UpsertAsync(name, json, timeProvider.GetLocalNow().DateTime);

    public async ValueTask<bool> DeleteAsync(string name)
    {
        var rows = await functionAccessor.DeleteAsync(name);
        return rows > 0;
    }

    // 呼び出し回数を加算して JSON を返す(未定義は null)
    public async ValueTask<string?> InvokeAsync(string name)
    {
        var entity = await functionAccessor.QueryAsync(name);
        if (entity is null)
        {
            return null;
        }

        await functionAccessor.IncrementCallAsync(name);
        return entity.Json;
    }
}
