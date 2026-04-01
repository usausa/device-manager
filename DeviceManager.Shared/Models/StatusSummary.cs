namespace DeviceManager.Shared.Models;

public sealed class StatusSummary
{
    public int Active { get; init; }
    public int Inactive { get; init; }
    public int Warning { get; init; }
    public int Error { get; init; }
    public int Total => Active + Inactive + Warning + Error;
}
