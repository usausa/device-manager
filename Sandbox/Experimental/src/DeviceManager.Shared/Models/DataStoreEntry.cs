namespace DeviceManager.Shared.Models;

public sealed class DataStoreEntry
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
