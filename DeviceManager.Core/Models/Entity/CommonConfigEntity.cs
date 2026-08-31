namespace DeviceManager.Models.Entity;

public sealed class CommonConfigEntity
{
    public string Key { get; set; } = default!;

    public string Value { get; set; } = default!;

    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; }
}
