namespace DeviceManager.Client.Telemetry;

using System.Threading.Channels;

// テレメトリの非同期送信チャネル。
// 送信はバックグラウンドで行い、失敗時はストアへタンキングして自動再送する(呼び出し側はブロックしない)
internal sealed class TelemetryChannel : IAsyncDisposable
{
    private readonly TelemetryOptions options;

    private readonly ITelemetryStore store;

    private readonly ITelemetryTransport transport;

    private readonly ILogger log;

    private readonly Channel<TelemetryEnvelope> queue;

    private readonly Channel<bool> signal;

    private readonly CancellationTokenSource cts = new();

    private readonly Task loopTask;

    private int consecutiveFailures;

    private DateTime nextAttemptAt = DateTime.MinValue;

    public TelemetryChannel(TelemetryOptions options, ITelemetryStore store, ITelemetryTransport transport, ILogger log)
    {
        this.options = options;
        this.store = store;
        this.transport = transport;
        this.log = log;

        // 書き込みは常に非ブロッキング(上限超過は古いものから破棄)
        queue = Channel.CreateBounded<TelemetryEnvelope>(new BoundedChannelOptions(options.QueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

        // 即時フラッシュ要求のシグナル
        signal = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        });

        loopTask = Task.Run(() => LoopAsync(cts.Token));
    }

    public async ValueTask DisposeAsync()
    {
        await cts.CancelAsync().ConfigureAwait(false);
        try
        {
            await loopTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // 停止
        }

        // 終了時はキュー残をストアへ退避する(SQLite プロバイダーなら次回起動時に再送される)
        try
        {
            while (queue.Reader.TryRead(out var envelope))
            {
                await store.AddAsync(envelope, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.WarnTelemetryStoreFailed(ex);
        }

        cts.Dispose();
    }

    //--------------------------------------------------------------------------------
    // Enqueue
    //--------------------------------------------------------------------------------

    // 非ブロッキングの投入。urgent は即時フラッシュを要求する(クラッシュレポート用)
    public void Enqueue(TelemetryKind kind, string payload, bool urgent = false)
    {
        queue.Writer.TryWrite(new TelemetryEnvelope
        {
            Kind = kind,
            Payload = payload,
            CreatedAt = DateTime.Now
        });

        if (urgent)
        {
            TriggerFlush();
        }
    }

    // 即時フラッシュを要求する(バックオフも解除。再接続検知時などに使用)
    public void TriggerFlush()
    {
        nextAttemptAt = DateTime.MinValue;
        signal.Writer.TryWrite(true);
    }

    public async ValueTask<int> GetPendingCountAsync(CancellationToken cancellationToken = default) =>
        queue.Reader.Count + await store.CountAsync(cancellationToken).ConfigureAwait(false);

    //--------------------------------------------------------------------------------
    // Pump
    //--------------------------------------------------------------------------------

    private bool InBackoff => DateTime.Now < nextAttemptAt;

    private void RegisterFailure()
    {
        consecutiveFailures++;
        var seconds = Math.Min(options.MaxBackoffSeconds, Math.Pow(2, consecutiveFailures));
        nextAttemptAt = DateTime.Now.AddSeconds(seconds);
    }

    private void RegisterSuccess()
    {
        consecutiveFailures = 0;
        nextAttemptAt = DateTime.MinValue;
    }

    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // シグナルまたはインターバル経過を待つ
            using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(options.FlushIntervalSeconds));
                try
                {
                    await signal.Reader.WaitToReadAsync(timeoutCts.Token).ConfigureAwait(false);
                    signal.Reader.TryRead(out _);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // インターバル経過
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await PumpAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask PumpAsync(CancellationToken cancellationToken)
    {
        try
        {
            // 保持期間・最大件数によるクリーンアップ
            await store.CleanupAsync(DateTime.Now.AddHours(-options.Store.RetentionHours), options.Store.MaxItems, cancellationToken).ConfigureAwait(false);

            // 新規分は先にストアへ退避してから送信する(persist-then-send)。
            // 送信中の中断・プロセス停止でも失われず、成功時にのみ削除される(at-least-once)
            while (queue.Reader.TryRead(out var envelope))
            {
                await store.AddAsync(envelope, cancellationToken).ConfigureAwait(false);
            }

            // ストア先頭(古い順)から送信し、成功分を削除する(バックオフ中はスキップ = 自動復旧の間隔制御)
            while (!InBackoff)
            {
                var batch = await store.PeekAsync(options.BatchSize, cancellationToken).ConfigureAwait(false);
                if (batch.Count == 0)
                {
                    break;
                }

                if (!await transport.SendAsync(batch, cancellationToken).ConfigureAwait(false))
                {
                    RegisterFailure();
                    break;
                }

                await store.RemoveAsync(batch.Select(static x => x.Id).ToList(), cancellationToken).ConfigureAwait(false);
                RegisterSuccess();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.WarnTelemetryStoreFailed(ex);
            RegisterFailure();
        }
    }
}
