namespace DeviceManager.Server.Models.Forms;

public sealed class DeviceForm
{
    public string DeviceId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? GroupName { get; set; }

    public string? Note { get; set; }

    public bool IsEnabled { get; set; } = true;
}
