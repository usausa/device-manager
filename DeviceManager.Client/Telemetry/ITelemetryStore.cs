namespace DeviceManager.Client.Telemetry;

// タンキングプロバイダー(送信できなかったテレメトリの保管と再送のためのストア)
public interface ITelemetryStore : IDisposable
{
    ValueTask AddAsync(TelemetryEnvelope envelope, CancellationToken cancellationToken = default);

    // 古い順に最大 max 件を取得する(削除はしない)
    ValueTask<IReadOnlyList<TelemetryEnvelope>> PeekAsync(int max, CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default);

    // 保持期間(threshold より古いもの)と最大件数を超えた分を削除する
    ValueTask CleanupAsync(DateTime threshold, int maxItems, CancellationToken cancellationToken = default);

    ValueTask<int> CountAsync(CancellationToken cancellationToken = default);
}
