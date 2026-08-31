namespace DeviceManager.Usecase;

using DeviceManager.Services;

public sealed class DeviceUsecase
{
    private readonly DeviceService deviceService;

    private readonly LogService logService;

    private readonly ErrorReportService errorReportService;

    private readonly MessageService messageService;

    private readonly ConfigService configService;

    private readonly TimeProvider timeProvider;

    public DeviceUsecase(
        DeviceService deviceService,
        LogService logService,
        ErrorReportService errorReportService,
        MessageService messageService,
        ConfigService configService,
        TimeProvider timeProvider)
    {
        this.deviceService = deviceService;
        this.logService = logService;
        this.errorReportService = errorReportService;
        this.messageService = messageService;
        this.configService = configService;
        this.timeProvider = timeProvider;
    }

    // ダッシュボードのサマリー値を集計する(稼働 = 期間内にステータス報告あり)
    public async ValueTask<DashboardSummary> QuerySummaryAsync(double lowBatteryThreshold)
    {
        var today = timeProvider.GetLocalNow().Date;
        var weekStart = today.AddDays(-6);

        var total = await deviceService.CountAsync();
        var activeDay = await deviceService.CountActiveSinceAsync(today);
        var activeWeek = await deviceService.CountActiveSinceAsync(weekStart);
        var lowBattery = await deviceService.CountLowBatterySinceAsync(lowBatteryThreshold, today);
        var warning = await deviceService.CountByStateAsync(DeviceState.Warning);
        var error = await deviceService.CountByStateAsync(DeviceState.Error);

        return new DashboardSummary(total, activeDay, activeWeek, lowBattery, warning, error);
    }

    // 端末と関連データ(ログ・エラーレポート・メッセージ・端末別コンフィグ)を削除する
    public async ValueTask<bool> DeleteDeviceAsync(string deviceId)
    {
        var deleted = await deviceService.DeleteAsync(deviceId);
        await logService.DeleteByDeviceAsync(deviceId);
        await errorReportService.DeleteByDeviceAsync(deviceId);
        await messageService.DeleteByDeviceAsync(deviceId);
        await configService.DeleteDeviceAsync(deviceId);
        return deleted;
    }
}
