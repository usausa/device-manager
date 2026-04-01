namespace DeviceManager.Shared.Models;

public sealed class DeviceUpdateRequest
{
    public string? Name { get; init; }
    public string? Group { get; init; }
    public string[]? Tags { get; init; }
    public string? Note { get; init; }
    public bool? IsEnabled { get; init; }
}
