namespace DeviceManager.TestClient;

using System.Windows;
using System.Windows.Media;

using DeviceManager.Client.Logging;
using DeviceManager.Client.Telemetry;

public partial class MainWindow : IAsyncDisposable
{
    private const int MaxLogItems = 500;

    private readonly ObservableCollection<string> logItems = [];

    private readonly RandomStatusProvider statusProvider = new();

    private DeviceManagerTelemetry? telemetry;

    private DeviceManagerClient? client;

    private ILoggerFactory? loggerFactory;

    public MainWindow()
    {
        InitializeComponent();
        LogList.ItemsSource = logItems;
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupAsync();
        GC.SuppressFinalize(this);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _ = DisposeAsync().AsTask();
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private void AppendLog(string message) =>
        Dispatcher.Invoke(() =>
        {
            logItems.Insert(0, $"{DateTime.Now:HH:mm:ss} {message}");
            while (logItems.Count > MaxLogItems)
            {
                logItems.RemoveAt(logItems.Count - 1);
            }
        });

    private void UpdateState(ConnectionState state) =>
        Dispatcher.Invoke(() =>
        {
            StateText.Text = state.ToString();
            StateIndicator.Background = state switch
            {
                ConnectionState.Connected => Brushes.LimeGreen,
                ConnectionState.Connecting => Brushes.Orange,
                ConnectionState.Reconnecting => Brushes.Orange,
                _ => Brushes.Gray
            };
            ConnectButton.IsEnabled = state == ConnectionState.Disconnected;
            DisconnectButton.IsEnabled = state != ConnectionState.Disconnected;
        });

    private string TestFilePath => $"devices/{DeviceIdBox.Text}/test.txt";

    private async ValueTask CleanupAsync()
    {
        try
        {
            if (client is not null)
            {
                await client.DisposeAsync();
                client = null;
            }

            if (telemetry is not null)
            {
                await telemetry.DisposeAsync();
                telemetry = null;
            }

            loggerFactory?.Dispose();
            loggerFactory = null;
        }
        catch (OperationCanceledException)
        {
            AppendLog("[キャンセル] 後始末を中断しました。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppendLog($"[エラー] 後始末に失敗しました: {ex.Message}");
        }
        finally
        {
            UpdateState(ConnectionState.Disconnected);
        }
    }

    //--------------------------------------------------------------------------------
    // Connection
    //--------------------------------------------------------------------------------

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void OnConnectClick(object sender, RoutedEventArgs e)
    {
        if (client is not null)
        {
            return;
        }

        try
        {
            var options = new DeviceManagerClientOptions
            {
                ServerUrl = ServerUrlBox.Text,
                ApiKey = ApiKeyBox.Text,
                Telemetry =
                {
                    GrpcUrl = GrpcUrlBox.Text,
                    FlushIntervalSeconds = 2,
                    LogMinLevel = DeviceLogLevel.Information,
                    // タンキングプロバイダーの選択(メモリ / SQLite)
                    Store = TankCombo.SelectedIndex == 1
                        ? new SqliteTelemetryStoreOptions { FilePath = Path.Combine(AppContext.BaseDirectory, "telemetry.db"), RetentionHours = 72, MaxItems = 10000 }
                        : new MemoryTelemetryStoreOptions { RetentionHours = 72, MaxItems = 10000 }
                }
            };

            var infoProvider = new TestDeviceInfoProvider(DeviceIdBox.Text, DeviceNameBox.Text);

            telemetry = new DeviceManagerTelemetry(options, infoProvider);

            // SDK のログ転送プロバイダーを組み込んだ LoggerFactory(ログテスト用)
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddProvider(new DeviceManagerLoggerProvider(options, telemetry));
            });

            client = new DeviceManagerClient(options, infoProvider, telemetry, loggerFactory.CreateLogger<DeviceManagerClient>(), statusProvider);
            client.StateChanged += (_, args) => UpdateState(args.State);
            client.MessageReceived += (_, args) => AppendLog($"[受信] {args.Message.MessageType}: {args.Message.Content}");
            client.ConfigReloaded += (_, args) => AppendLog($"[コンフィグ] {String.Join(", ", args.Values.Select(static x => $"{x.Key}={x.Value}"))}");

            await client.ConnectAsync();
            AppendLog("接続しました。");
        }
        catch (OperationCanceledException)
        {
            AppendLog("[キャンセル] 接続を中断しました。");
            await CleanupAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppendLog($"[エラー] 接続失敗: {ex.Message}");
            await CleanupAsync();
        }
    }

    private async void OnDisconnectClick(object sender, RoutedEventArgs e)
    {
        try
        {
            AutoStatusCheck.IsChecked = false;
            await CleanupAsync();
            AppendLog("切断しました。");
        }
        catch (OperationCanceledException)
        {
            AppendLog("[キャンセル] 操作を中断しました。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppendLog($"[エラー] {ex.Message}");
        }
    }

    //--------------------------------------------------------------------------------
    // Status
    //--------------------------------------------------------------------------------

    private void OnAutoStatusChecked(object sender, RoutedEventArgs e)
    {
        try
        {
            client?.StartStatusReporting(TimeSpan.FromSeconds(5));
            AppendLog("自動ステータス報告を開始しました。");
        }
        catch (InvalidOperationException ex)
        {
            AppendLog($"[エラー] {ex.Message}");
        }
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void OnAutoStatusUnchecked(object sender, RoutedEventArgs e)
    {
        if (client is null)
        {
            return;
        }

        try
        {
            await client.StopStatusReportingAsync();
            AppendLog("自動ステータス報告を停止しました。");
        }
        catch (OperationCanceledException)
        {
            AppendLog("[キャンセル] 操作を中断しました。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppendLog($"[エラー] {ex.Message}");
        }
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void OnSendStatusClick(object sender, RoutedEventArgs e)
    {
        if (client is null)
        {
            return;
        }

        try
        {
            var report = await statusProvider.GetStatusAsync();
            client.ReportStatus(report);
            AppendLog($"[ステータス] battery={report.Battery:F0}% rssi={report.WifiRssi}dB ap={report.ApName} scan={report.ScanCount}(バックグラウンドで gRPC 送信)");
        }
        catch (OperationCanceledException)
        {
            AppendLog("[キャンセル] 操作を中断しました。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppendLog($"[エラー] {ex.Message}");
        }
    }

    //--------------------------------------------------------------------------------
    // Message
    //--------------------------------------------------------------------------------

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void OnSendMessageClick(object sender, RoutedEventArgs e)
    {
        if (client is null)
        {
            return;
        }

        try
        {
            await client.SendMessageAsync(MessageTypeBox.Text, MessageContentBox.Text);
            AppendLog($"[送信] {MessageTypeBox.Text}: {MessageContentBox.Text}");
        }
        catch (OperationCanceledException)
        {
            AppendLog("[キャンセル] 操作を中断しました。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppendLog($"[エラー] {ex.Message}");
        }
    }

    //--------------------------------------------------------------------------------
    // Storage
    //--------------------------------------------------------------------------------

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void OnUploadClick(object sender, RoutedEventArgs e)
    {
        if (client is null)
        {
            return;
        }

        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes($"Test upload at {DateTime.Now:yyyy/MM/dd HH:mm:ss}"));
            await client.UploadAsync(TestFilePath, stream);
            AppendLog($"[ストレージ] アップロード: {TestFilePath}");
        }
        catch (OperationCanceledException)
        {
            AppendLog("[キャンセル] 操作を中断しました。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppendLog($"[エラー] {ex.Message}");
        }
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void OnListClick(object sender, RoutedEventArgs e)
    {
        if (client is null)
        {
            return;
        }

        try
        {
            var entries = await client.ListAsync($"devices/{DeviceIdBox.Text}");
            AppendLog($"[ストレージ] {entries.Count} 件: {String.Join(", ", entries.Select(static x => x.Name))}");
        }
        catch (OperationCanceledException)
        {
            AppendLog("[キャンセル] 操作を中断しました。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppendLog($"[エラー] {ex.Message}");
        }
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        if (client is null)
        {
            return;
        }

        try
        {
            var stream = await client.DownloadAsync(TestFilePath);
            if (stream is null)
            {
                AppendLog("[ストレージ] ファイルがありません。");
                return;
            }

            await using (stream)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var text = await reader.ReadToEndAsync();
                AppendLog($"[ストレージ] ダウンロード: {text}");
            }
        }
        catch (OperationCanceledException)
        {
            AppendLog("[キャンセル] 操作を中断しました。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppendLog($"[エラー] {ex.Message}");
        }
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (client is null)
        {
            return;
        }

        try
        {
            var deleted = await client.DeleteAsync(TestFilePath);
            AppendLog(deleted ? "[ストレージ] 削除しました。" : "[ストレージ] ファイルがありません。");
        }
        catch (OperationCanceledException)
        {
            AppendLog("[キャンセル] 操作を中断しました。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppendLog($"[エラー] {ex.Message}");
        }
    }

    //--------------------------------------------------------------------------------
    // Crash report / Log / Config
    //--------------------------------------------------------------------------------

    private void OnErrorReportClick(object sender, RoutedEventArgs e)
    {
        if (client is null)
        {
            return;
        }

        try
        {
            // テスト用の例外を発生させて送信する
            throw new InvalidOperationException("Test exception from TestClient.");
        }
        catch (InvalidOperationException ex)
        {
            client.ReportCrash(ex, "1.0.0", Environment.OSVersion.ToString());
            AppendLog("[クラッシュレポート] 送信キューへ投入しました(即時フラッシュ)。");
        }
    }

    private void OnLogTestClick(object sender, RoutedEventArgs e)
    {
        if (loggerFactory is null)
        {
            return;
        }

        var logger = loggerFactory.CreateLogger("TestClient.Sample");
        logger.InfoTest(logItems.Count);
        logger.WarnTest();
        logger.ErrorTest(new InvalidOperationException("Test error for log."));
        AppendLog("[ログ] テストログを 3 件出力しました(バックグラウンドで gRPC 転送されます)。");
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void OnGetConfigClick(object sender, RoutedEventArgs e)
    {
        if (client is null)
        {
            return;
        }

        try
        {
            var values = await client.GetConfigAsync();
            AppendLog($"[コンフィグ] {values.Count} 件: {String.Join(", ", values.Select(static x => $"{x.Key}={x.Value}"))}");
        }
        catch (OperationCanceledException)
        {
            AppendLog("[キャンセル] 操作を中断しました。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppendLog($"[エラー] {ex.Message}");
        }
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void OnPendingCountClick(object sender, RoutedEventArgs e)
    {
        if (telemetry is null)
        {
            return;
        }

        try
        {
            var count = await telemetry.GetPendingCountAsync();
            AppendLog($"[テレメトリ] 未送信 {count} 件(キュー + タンク)");
        }
        catch (OperationCanceledException)
        {
            AppendLog("[キャンセル] 操作を中断しました。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AppendLog($"[エラー] {ex.Message}");
        }
    }
}
