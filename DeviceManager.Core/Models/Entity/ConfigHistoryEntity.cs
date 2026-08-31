namespace DeviceManager.Models.Entity;

public sealed class ConfigHistoryEntity
{
    public long Id { get; set; }

    // common または device:{DeviceId}
    public string Scope { get; set; } = default!;

    public string Key { get; set; } = default!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime ChangedAt { get; set; }
}
