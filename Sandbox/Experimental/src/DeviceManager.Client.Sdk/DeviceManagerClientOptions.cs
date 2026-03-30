namespace DeviceManager.Client.Sdk;

public sealed class DeviceManagerClientOptions
{
    public required string ServerUrl { get; set; }
    public bool AutoReconnect { get; set; } = true;
    public TimeSpan MaxReconnectInterval { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan DefaultStatusInterval { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan ApiTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public string? ConfigCachePath { get; set; }
    public bool UseGrpc { get; set; } = true;
    public string? GrpcUrl { get; set; }
}
