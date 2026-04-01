namespace DeviceManager.Shared.Models;

public sealed class ConfigEntry
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public string ValueType { get; init; } = "string";
    public string? Description { get; init; }
}
