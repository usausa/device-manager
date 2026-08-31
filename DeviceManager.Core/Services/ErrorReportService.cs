namespace DeviceManager.Services;

using DeviceManager.Accessors;

public sealed class ErrorReportService
{
    private readonly ErrorReportAccessor errorReportAccessor;

    private readonly TimeProvider timeProvider;

    public ErrorReportService(
        ErrorReportAccessor errorReportAccessor,
        TimeProvider timeProvider)
    {
        this.errorReportAccessor = errorReportAccessor;
        this.timeProvider = timeProvider;
    }

    public void CreateTable() =>
        errorReportAccessor.Create();

    public ValueTask<long> AddAsync(ErrorReportRequest request) =>
        errorReportAccessor.InsertAsync(
            request.DeviceId,
            request.ExceptionType,
            request.Message,
            request.StackTrace,
            request.InnerException,
            request.AppVersion,
            request.OsVersion,
            request.OccurredAt,
            timeProvider.GetLocalNow().DateTime);

    public ValueTask<int> CountAsync(string? deviceId) =>
        errorReportAccessor.CountAsync(String.IsNullOrEmpty(deviceId) ? null : deviceId);

    public ValueTask<List<ErrorReportEntity>> QueryPageAsync(string? deviceId, int offset, int size) =>
        errorReportAccessor.QueryPageAsync(String.IsNullOrEmpty(deviceId) ? null : deviceId, offset, size);

    public ValueTask<ErrorReportEntity?> QueryAsync(long reportId) =>
        errorReportAccessor.QueryAsync(reportId);

    public async ValueTask<bool> DeleteAsync(long reportId)
    {
        var rows = await errorReportAccessor.DeleteAsync(reportId);
        return rows > 0;
    }

    public ValueTask<int> DeleteByDeviceAsync(string deviceId) =>
        errorReportAccessor.DeleteByDeviceAsync(deviceId);

    public ValueTask<int> CleanupAsync(DateTime threshold) =>
        errorReportAccessor.DeleteOlderAsync(threshold);
}
