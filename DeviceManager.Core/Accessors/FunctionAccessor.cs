namespace DeviceManager.Accessors;

[DataAccessor]
public sealed partial class FunctionAccessor
{
    [Execute]
    public partial void Create();

    [Query]
    public partial ValueTask<List<FunctionEntity>> QueryAllAsync();

    [QueryFirst]
    public partial ValueTask<FunctionEntity?> QueryAsync(string name);

    [Execute]
    public partial ValueTask<int> UpsertAsync(string name, string json, DateTime updatedAt);

    [Execute]
    public partial ValueTask<int> DeleteAsync(string name);

    [Execute]
    public partial ValueTask<int> IncrementCallAsync(string name);
}
