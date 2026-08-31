namespace DeviceManager.Contracts;

/// <summary>
/// コンフィグ値。
/// </summary>
public sealed class ConfigValue
{
    /// <summary>キー。</summary>
    public string Key { get; set; } = default!;

    /// <summary>値。</summary>
    public string Value { get; set; } = default!;
}
