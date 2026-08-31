namespace DeviceManager.Client.Telemetry;

// オンメモリのタンキングプロバイダー
public sealed class MemoryTelemetryStore : ITelemetryStore
{
    private readonly MemoryTelemetryStoreOptions options;

    private readonly Lock sync = new();

    private readonly List<TelemetryEnvelope> items = [];

    private long nextId = 1;

    public MemoryTelemetryStore(MemoryTelemetryStoreOptions options)
    {
        this.options = options;
    }

    public void Dispose()
    {
        // 保持リソースなし
    }

    public ValueTask AddAsync(TelemetryEnvelope envelope, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            envelope.Id = nextId++;
            items.Add(envelope);

            // 上限超過は古いものから破棄
            if (items.Count > options.MaxItems)
            {
                items.RemoveRange(0, items.Count - options.MaxItems);
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<TelemetryEnvelope>> PeekAsync(int max, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return ValueTask.FromResult<IReadOnlyList<TelemetryEnvelope>>([.. items.Take(max)]);
        }
    }

    public ValueTask RemoveAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var set = ids.ToHashSet();
            items.RemoveAll(x => set.Contains(x.Id));
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask CleanupAsync(DateTime threshold, int maxItems, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            items.RemoveAll(x => x.CreatedAt < threshold);
            if (items.Count > maxItems)
            {
                items.RemoveRange(0, items.Count - maxItems);
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return ValueTask.FromResult(items.Count);
        }
    }
}
