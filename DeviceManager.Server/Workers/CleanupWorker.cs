namespace DeviceManager.Server.Workers;

using DeviceManager.Server.Application;

// 履歴系データを保持期間に基づいて定期削除する
public sealed class CleanupWorker : BackgroundService
{
    private readonly ILogger<CleanupWorker> log;

    private readonly CleanupSetting setting;

    private readonly DeviceService deviceService;

    private readonly LogService logService;

    private readonly ErrorReportService errorReportService;

    private readonly TimeProvider timeProvider;

    public CleanupWorker(
        ILogger<CleanupWorker> log,
        CleanupSetting setting,
        DeviceService deviceService,
        LogService logService,
        ErrorReportService errorReportService,
        TimeProvider timeProvider)
    {
        this.log = log;
        this.setting = setting;
        this.deviceService = deviceService;
        this.logService = logService;
        this.errorReportService = errorReportService;
        this.timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!setting.Enable)
        {
            log.InfoWorkerDisabled(nameof(CleanupWorker));
            return;
        }

        log.InfoWorkerStart(nameof(CleanupWorker));
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(setting.IntervalSeconds));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    var threshold = timeProvider.GetLocalNow().DateTime.AddDays(-setting.RetentionDays);
                    await deviceService.CleanupAsync(threshold);
                    await logService.CleanupAsync(threshold);
                    await errorReportService.CleanupAsync(threshold);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.ErrorUnhandledException(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown
        }
        finally
        {
            log.InfoWorkerStop(nameof(CleanupWorker));
        }
    }
}
