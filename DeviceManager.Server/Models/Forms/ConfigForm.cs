namespace DeviceManager.Server.Models.Forms;

public sealed class ConfigForm
{
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string? Description { get; set; }
}
