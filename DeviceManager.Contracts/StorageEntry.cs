namespace DeviceManager.Contracts;

/// <summary>
/// ストレージのエントリ情報。
/// </summary>
public sealed class StorageEntry
{
    /// <summary>名前。</summary>
    public string Name { get; set; } = default!;

    /// <summary>ディレクトリか。</summary>
    public bool Directory { get; set; }

    /// <summary>サイズ(バイト)。ディレクトリは 0。</summary>
    public long Size { get; set; }

    /// <summary>最終更新日時。</summary>
    public DateTime LastModified { get; set; }
}
