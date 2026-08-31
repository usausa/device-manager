namespace DeviceManager.Models.Entity;

public sealed class FunctionEntity
{
    public string Name { get; set; } = default!;

    public string Json { get; set; } = default!;

    public long CallCount { get; set; }

    public DateTime UpdatedAt { get; set; }
}
