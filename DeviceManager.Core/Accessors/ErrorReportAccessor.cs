namespace DeviceManager.Accessors;

[DataAccessor]
public sealed partial class ErrorReportAccessor
{
    [Execute]
    public partial void Create();

    [ExecuteScalar]
    public partial ValueTask<long> InsertAsync(string deviceId, string exceptionType, string message, string? stackTrace, string? innerException, string? appVersion, string? osVersion, DateTime occurredAt, DateTime receivedAt);

    [ExecuteScalar]
    public partial ValueTask<int> CountAsync(string? deviceId);

    [Query]
    public partial ValueTask<List<ErrorReportEntity>> QueryPageAsync(string? deviceId, int offset, int size);

    [QueryFirst]
    public partial ValueTask<ErrorReportEntity?> QueryAsync(long reportId);

    [Execute]
    public partial ValueTask<int> DeleteAsync(long reportId);

    [Execute]
    public partial ValueTask<int> DeleteByDeviceAsync(string deviceId);

    [Execute]
    public partial ValueTask<int> DeleteOlderAsync(DateTime threshold);
}
