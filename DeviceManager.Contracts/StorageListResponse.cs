namespace DeviceManager.Contracts;

/// <summary>
/// ストレージの一覧レスポンス。
/// </summary>
#pragma warning disable CA1002
public sealed class StorageListResponse
{
    /// <summary>エントリ。</summary>
    public List<StorageEntry> Entries { get; init; } = [];
}
#pragma warning restore CA1002
