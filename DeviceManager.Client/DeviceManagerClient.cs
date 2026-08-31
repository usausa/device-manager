namespace DeviceManager.Client;

using DeviceManager.Client.Telemetry;

using Microsoft.AspNetCore.SignalR.Client;

// 端末側からサーバーと通信するクライアント。
// 接続 / メッセージ / コンフィグ配信は SignalR、ストレージ / コンフィグ取得は HTTP、
// テレメトリ(メトリクス / ログ / クラッシュレポート)は DeviceManagerTelemetry(gRPC)を使用する
public sealed class DeviceManagerClient : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly DeviceManagerClientOptions options;

    private readonly IDeviceInfoProvider infoProvider;

    private readonly DeviceManagerTelemetry telemetry;

    private readonly IDeviceStatusProvider? statusProvider;

    private readonly ILogger<DeviceManagerClient> log;

    private readonly HubConnection connection;

    private readonly HttpClient httpClient;

    private CancellationTokenSource? statusCts;

    private Task? statusTask;

    public string DeviceId => infoProvider.DeviceId;

    public ConnectionState State { get; private set; }

    public event EventHandler<ConnectionStateEventArgs>? StateChanged;

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public event EventHandler<ConfigReloadedEventArgs>? ConfigReloaded;

    public DeviceManagerClient(
        DeviceManagerClientOptions options,
        IDeviceInfoProvider infoProvider,
        DeviceManagerTelemetry telemetry,
        ILogger<DeviceManagerClient> log,
        IDeviceStatusProvider? statusProvider = null)
    {
        this.options = options;
        this.infoProvider = infoProvider;
        this.telemetry = telemetry;
        this.statusProvider = statusProvider;
        this.log = log;

        var baseUri = new Uri(options.ServerUrl.EndsWith('/') ? options.ServerUrl : options.ServerUrl + "/");

        httpClient = new HttpClient
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromSeconds(options.ApiTimeoutSeconds)
        };
        httpClient.DefaultRequestHeaders.Add(ApiConstants.ApiKeyHeader, options.ApiKey);

        var builder = new HubConnectionBuilder()
            .WithUrl(new Uri(baseUri, DeviceHubConstants.Path.TrimStart('/')), o => o.Headers.Add(ApiConstants.ApiKeyHeader, options.ApiKey));
        if (options.AutoReconnect)
        {
            builder = builder.WithAutomaticReconnect(new BackoffRetryPolicy(TimeSpan.FromSeconds(options.MaxReconnectIntervalSeconds)));
        }

        connection = builder.Build();
        connection.On<DeviceMessage>(DeviceHubConstants.Callbacks.ReceiveMessage, message => MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message)));
        connection.On<List<ConfigValue>>(DeviceHubConstants.Callbacks.ConfigReload, values => ConfigReloaded?.Invoke(this, new ConfigReloadedEventArgs(values)));
        connection.Reconnecting += _ =>
        {
            SetState(ConnectionState.Reconnecting);
            return Task.CompletedTask;
        };
        connection.Reconnected += async _ =>
        {
            // 再接続時は端末を再登録し、タンク済みテレメトリの即時再送を要求する
            await RegisterAsync(default).ConfigureAwait(false);
            telemetry.TriggerFlush();
            SetState(ConnectionState.Connected);
        };
        connection.Closed += _ =>
        {
            SetState(ConnectionState.Disconnected);
            return Task.CompletedTask;
        };
    }

    public async ValueTask DisposeAsync()
    {
        await StopStatusReportingAsync().ConfigureAwait(false);
        await connection.DisposeAsync().ConfigureAwait(false);
        httpClient.Dispose();
    }

    private void SetState(ConnectionState state)
    {
        if (State == state)
        {
            return;
        }

        State = state;
        StateChanged?.Invoke(this, new ConnectionStateEventArgs(state));
    }

    //--------------------------------------------------------------------------------
    // Connection
    //--------------------------------------------------------------------------------

    public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        SetState(ConnectionState.Connecting);
        try
        {
            await connection.StartAsync(cancellationToken).ConfigureAwait(false);
            await RegisterAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            SetState(ConnectionState.Disconnected);
            throw;
        }

        SetState(ConnectionState.Connected);
        telemetry.TriggerFlush();
        log.InfoConnected(infoProvider.DeviceId);
    }

    public async ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await StopStatusReportingAsync().ConfigureAwait(false);
        await connection.StopAsync(cancellationToken).ConfigureAwait(false);
        SetState(ConnectionState.Disconnected);
        log.InfoDisconnected();
    }

    private Task RegisterAsync(CancellationToken cancellationToken) =>
        connection.InvokeAsync(
            DeviceHubConstants.Methods.Register,
            new DeviceRegistration
            {
                DeviceId = infoProvider.DeviceId,
                Name = infoProvider.DeviceName,
                Platform = infoProvider.Platform
            },
            cancellationToken);

    //--------------------------------------------------------------------------------
    // Telemetry
    //--------------------------------------------------------------------------------

    // メトリクスの送信(gRPC。非ブロッキング、失敗時はタンキングして自動再送)
    public void ReportStatus(DeviceStatusReport report) =>
        telemetry.ReportStatus(report);

    // クラッシュレポートの送信(gRPC。非ブロッキング)
    public void ReportCrash(Exception exception, string? appVersion = null, string? osVersion = null) =>
        telemetry.ReportCrash(exception, appVersion, osVersion);

    public void ReportCrash(ErrorReportRequest request) =>
        telemetry.ReportCrash(request);

    // 定期ステータス報告を開始する(取得例外はブロックしない)
    public void StartStatusReporting(TimeSpan? interval = null)
    {
        if (statusProvider is null)
        {
            throw new InvalidOperationException("Status provider is not configured.");
        }

        if (statusTask is not null)
        {
            return;
        }

        statusCts = new CancellationTokenSource();
        statusTask = ReportLoopAsync(interval ?? TimeSpan.FromSeconds(options.StatusIntervalSeconds), statusCts.Token);
    }

    public async ValueTask StopStatusReportingAsync()
    {
        if (statusTask is null)
        {
            return;
        }

        try
        {
            await statusCts!.CancelAsync().ConfigureAwait(false);
            await statusTask.ConfigureAwait(false);
        }
        finally
        {
            statusCts!.Dispose();
            statusCts = null;
            statusTask = null;
        }
    }

    private async Task ReportLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    var report = await statusProvider!.GetStatusAsync(cancellationToken).ConfigureAwait(false);
                    telemetry.ReportStatus(report);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.WarnStatusReportFailed(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 停止
        }
    }

    //--------------------------------------------------------------------------------
    // Message
    //--------------------------------------------------------------------------------

    public ValueTask SendMessageAsync(string messageType, string content, CancellationToken cancellationToken = default) =>
        new(connection.InvokeAsync(DeviceHubConstants.Methods.SendMessage, messageType, content, cancellationToken));

    //--------------------------------------------------------------------------------
    // Storage
    //--------------------------------------------------------------------------------

    private static string EncodePath(string path) =>
        String.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    public async ValueTask UploadAsync(string path, Stream stream, CancellationToken cancellationToken = default)
    {
        using var content = new StreamContent(stream);
        using var response = await httpClient.PostAsync(new Uri($"api/storage/{EncodePath(path)}", UriKind.Relative), content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    // ダウンロード(存在しない場合は null)
    public async ValueTask<Stream?> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(new Uri($"api/storage/{EncodePath(path)}", UriKind.Relative), HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<StorageEntry>> ListAsync(string path, CancellationToken cancellationToken = default)
    {
        var target = path.Length == 0 ? "api/storage/" : $"api/storage/{EncodePath(path)}/";
        var response = await httpClient.GetFromJsonAsync<StorageListResponse>(new Uri(target, UriKind.Relative), JsonOptions, cancellationToken).ConfigureAwait(false);
        return response?.Entries ?? [];
    }

    public async ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(new Uri($"api/storage/{EncodePath(path)}", UriKind.Relative), cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    //--------------------------------------------------------------------------------
    // Config
    //--------------------------------------------------------------------------------

    // 解決済み(共通 + 端末別上書き)のコンフィグを取得する
    public async ValueTask<IReadOnlyList<ConfigValue>> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var values = await httpClient.GetFromJsonAsync<List<ConfigValue>>(new Uri($"api/config/devices/{Uri.EscapeDataString(infoProvider.DeviceId)}/resolved", UriKind.Relative), JsonOptions, cancellationToken).ConfigureAwait(false);
        return values ?? [];
    }
}
