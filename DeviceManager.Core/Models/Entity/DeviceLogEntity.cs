namespace DeviceManager.Models.Entity;

public sealed class DeviceLogEntity
{
    public long LogId { get; set; }

    public string DeviceId { get; set; } = default!;

    // DeviceLogLevel の値
    public int Level { get; set; }

    public string Category { get; set; } = default!;

    public string Message { get; set; } = default!;

    public string? Exception { get; set; }

    public DateTime CreatedAt { get; set; }
}
