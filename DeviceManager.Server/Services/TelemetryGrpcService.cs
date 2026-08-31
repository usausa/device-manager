namespace DeviceManager.Server.Services;

using DeviceManager.Server.Infrastructure.Authentication;
using DeviceManager.Server.Infrastructure.Notifications;
using DeviceManager.Telemetry;

using Grpc.Core;

// テレメトリ(メトリクス / ログ / クラッシュレポート)の gRPC 受信サービス(API キー認証)
[Authorize(AuthenticationSchemes = ApiKeyDefaults.AuthenticationScheme)]
public sealed class TelemetryGrpcService : TelemetryService.TelemetryServiceBase
{
    private readonly DeviceService deviceService;

    private readonly LogService logService;

    private readonly ErrorReportService errorReportService;

    private readonly DeviceEventBus eventBus;

    private readonly TimeProvider timeProvider;

    public TelemetryGrpcService(
        DeviceService deviceService,
        LogService logService,
        ErrorReportService errorReportService,
        DeviceEventBus eventBus,
        TimeProvider timeProvider)
    {
        this.deviceService = deviceService;
        this.logService = logService;
        this.errorReportService = errorReportService;
        this.eventBus = eventBus;
        this.timeProvider = timeProvider;
    }

    // Unix ミリ秒をローカル時刻へ変換する(未設定は現在時刻)
    private DateTime ToLocalTime(long unixMilliseconds) =>
        unixMilliseconds <= 0
            ? timeProvider.GetLocalNow().DateTime
            : DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).LocalDateTime;

    public override async Task<TelemetryReply> ReportStatus(StatusRequest request, ServerCallContext context)
    {
        await deviceService.EnsureAsync(request.DeviceId);

        var report = new DeviceStatusReport
        {
            Level = request.Level,
            Battery = request.Battery,
            WifiRssi = request.WifiRssi,
            ApName = request.ApName.Length == 0 ? null : request.ApName,
            Moving = request.Moving,
            ScanCount = request.ScanCount,
            Progress1 = request.Progress1,
            Progress2 = request.Progress2,
            Latitude = request.HasLatitude ? request.Latitude : null,
            Longitude = request.HasLongitude ? request.Longitude : null
        };
        await deviceService.ReportStatusAsync(request.DeviceId, report, ToLocalTime(request.Timestamp));

        eventBus.Publish(DeviceEventType.StatusUpdated, request.DeviceId);
        return new TelemetryReply { Accepted = true };
    }

    public override async Task<TelemetryReply> SendLogs(LogRequest request, ServerCallContext context)
    {
        await deviceService.EnsureAsync(request.DeviceId);

        var records = request.Records
            .Select(x => new DeviceLogRecord
            {
                Level = (DeviceLogLevel)x.Level,
                Category = x.Category,
                Message = x.Message,
                Exception = x.Exception.Length == 0 ? null : x.Exception,
                Timestamp = ToLocalTime(x.Timestamp)
            })
            .ToList();
        await logService.AddRangeAsync(request.DeviceId, records);

        eventBus.Publish(DeviceEventType.LogReceived, request.DeviceId);
        return new TelemetryReply { Accepted = true };
    }

    public override async Task<TelemetryReply> SendCrashReport(CrashReportRequest request, ServerCallContext context)
    {
        await deviceService.EnsureAsync(request.DeviceId);

        await errorReportService.AddAsync(new ErrorReportRequest
        {
            DeviceId = request.DeviceId,
            ExceptionType = request.ExceptionType,
            Message = request.Message,
            StackTrace = request.StackTrace.Length == 0 ? null : request.StackTrace,
            InnerException = request.InnerException.Length == 0 ? null : request.InnerException,
            AppVersion = request.AppVersion.Length == 0 ? null : request.AppVersion,
            OsVersion = request.OsVersion.Length == 0 ? null : request.OsVersion,
            OccurredAt = ToLocalTime(request.OccurredAt)
        });

        eventBus.Publish(DeviceEventType.ErrorReportReceived, request.DeviceId);
        return new TelemetryReply { Accepted = true };
    }
}
