namespace DeviceManager.Client.Telemetry;

using DeviceManager.Telemetry;

using Grpc.Core;
using Grpc.Net.Client;

// gRPC によるテレメトリ送信(メトリクス / ログ / クラッシュレポート)
internal sealed class TelemetryGrpcTransport : ITelemetryTransport
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDeviceInfoProvider infoProvider;

    private readonly GrpcChannel channel;

    private readonly TelemetryService.TelemetryServiceClient client;

    private readonly Metadata headers;

    private readonly TimeSpan timeout;

    public TelemetryGrpcTransport(DeviceManagerClientOptions options, IDeviceInfoProvider infoProvider)
    {
        if (String.IsNullOrEmpty(options.Telemetry.GrpcUrl))
        {
            throw new InvalidOperationException("Telemetry.GrpcUrl is not configured.");
        }

        this.infoProvider = infoProvider;

        channel = GrpcChannel.ForAddress(options.Telemetry.GrpcUrl);
        client = new TelemetryService.TelemetryServiceClient(channel);
        headers = [new Metadata.Entry(ApiConstants.ApiKeyHeader, options.ApiKey)];
        timeout = TimeSpan.FromSeconds(options.ApiTimeoutSeconds);
    }

    public void Dispose() =>
        channel.Dispose();

    private CallOptions CreateCallOptions(CancellationToken cancellationToken) =>
        new(headers, DateTime.UtcNow.Add(timeout), cancellationToken);

    private static long ToUnixMilliseconds(DateTime value) =>
        new DateTimeOffset(value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Local) : value).ToUnixTimeMilliseconds();

    // 一括送信(ログはまとめて 1 リクエスト、メトリクス / クラッシュは順に送信)。通信失敗は false
    public async ValueTask<bool> SendAsync(IReadOnlyList<TelemetryEnvelope> envelopes, CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = new List<LogRecord>();
            foreach (var envelope in envelopes)
            {
                try
                {
                    switch (envelope.Kind)
                    {
                        case TelemetryKind.Metric:
                            var report = JsonSerializer.Deserialize<DeviceStatusReport>(envelope.Payload, JsonOptions);
                            if (report is not null)
                            {
                                await client.ReportStatusAsync(ToStatusRequest(report, envelope.CreatedAt), CreateCallOptions(cancellationToken)).ConfigureAwait(false);
                            }

                            break;
                        case TelemetryKind.CrashReport:
                            var crash = JsonSerializer.Deserialize<ErrorReportRequest>(envelope.Payload, JsonOptions);
                            if (crash is not null)
                            {
                                await client.SendCrashReportAsync(ToCrashRequest(crash), CreateCallOptions(cancellationToken)).ConfigureAwait(false);
                            }

                            break;
                        default:
                            var record = JsonSerializer.Deserialize<DeviceLogRecord>(envelope.Payload, JsonOptions);
                            if (record is not null)
                            {
                                logs.Add(ToLogRecord(record));
                            }

                            break;
                    }
                }
                catch (JsonException)
                {
                    // 壊れたペイロードは破棄(ポイズンメッセージ対策)
                }
            }

            if (logs.Count > 0)
            {
                var request = new LogRequest { DeviceId = infoProvider.DeviceId };
                request.Records.AddRange(logs);
                await client.SendLogsAsync(request, CreateCallOptions(cancellationToken)).ConfigureAwait(false);
            }

            return true;
        }
        catch (RpcException)
        {
            return false;
        }
    }

    private StatusRequest ToStatusRequest(DeviceStatusReport report, DateTime measuredAt)
    {
        var request = new StatusRequest
        {
            DeviceId = infoProvider.DeviceId,
            Level = report.Level,
            Battery = report.Battery,
            WifiRssi = report.WifiRssi,
            ApName = report.ApName ?? string.Empty,
            Moving = report.Moving,
            ScanCount = report.ScanCount,
            Progress1 = report.Progress1,
            Progress2 = report.Progress2,
            Timestamp = ToUnixMilliseconds(measuredAt)
        };
        if (report.Latitude is not null)
        {
            request.Latitude = report.Latitude.Value;
        }

        if (report.Longitude is not null)
        {
            request.Longitude = report.Longitude.Value;
        }

        return request;
    }

    private CrashReportRequest ToCrashRequest(ErrorReportRequest crash) =>
        new()
        {
            DeviceId = String.IsNullOrEmpty(crash.DeviceId) ? infoProvider.DeviceId : crash.DeviceId,
            ExceptionType = crash.ExceptionType,
            Message = crash.Message,
            StackTrace = crash.StackTrace ?? string.Empty,
            InnerException = crash.InnerException ?? string.Empty,
            AppVersion = crash.AppVersion ?? string.Empty,
            OsVersion = crash.OsVersion ?? string.Empty,
            OccurredAt = ToUnixMilliseconds(crash.OccurredAt)
        };

    private static LogRecord ToLogRecord(DeviceLogRecord record) =>
        new()
        {
            Level = (int)record.Level,
            Category = record.Category,
            Message = record.Message,
            Exception = record.Exception ?? string.Empty,
            Timestamp = ToUnixMilliseconds(record.Timestamp)
        };
}
