namespace DeviceManager.Shared.Models;

public sealed class DeviceRegistration
{
    public required string DeviceId { get; init; }
    public required string Name { get; init; }
    public string? Platform { get; init; }
    public string? Group { get; init; }
    public IDictionary<string, string>? AdditionalInfo { get; init; }
}
