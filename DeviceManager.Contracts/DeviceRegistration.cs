namespace DeviceManager.Contracts;

/// <summary>
/// 端末登録情報。
/// </summary>
public sealed class DeviceRegistration
{
    /// <summary>端末ID。</summary>
    [Required]
    [MaxLength(64)]
    public string DeviceId { get; set; } = default!;

    /// <summary>端末名。</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;

    /// <summary>プラットフォーム。</summary>
    [MaxLength(50)]
    public string? Platform { get; set; }
}
