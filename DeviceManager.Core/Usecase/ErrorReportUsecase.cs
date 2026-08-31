namespace DeviceManager.Usecase;

using DeviceManager.Services;

public sealed class ErrorReportUsecase
{
    private readonly ErrorReportService errorReportService;

    public ErrorReportUsecase(ErrorReportService errorReportService)
    {
        this.errorReportService = errorReportService;
    }

    public async ValueTask<PagedResult<ErrorReportEntity>> QueryPageAsync(string? deviceId, int page, int size)
    {
        var total = await errorReportService.CountAsync(deviceId);
        var items = await errorReportService.QueryPageAsync(deviceId, page * size, size);
        return new PagedResult<ErrorReportEntity>(total, page, size, items);
    }
}
