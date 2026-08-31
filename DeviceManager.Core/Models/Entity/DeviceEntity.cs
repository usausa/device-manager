namespace DeviceManager.Models.Entity;

public sealed class DeviceEntity
{
    public string DeviceId { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string? Platform { get; set; }

    public string? GroupName { get; set; }

    public string? Note { get; set; }

    // 0:無効 / 1:有効
    public int IsEnabled { get; set; }

    // DeviceState の値
    public int Status { get; set; }

    public DateTime RegisteredAt { get; set; }

    public DateTime? LastConnectedAt { get; set; }
}
