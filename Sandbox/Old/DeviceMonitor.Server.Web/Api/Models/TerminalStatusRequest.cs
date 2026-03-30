namespace DeviceMonitor.Server.Web.Api.Models;

public class TerminalStatusRequest
{
    public Guid Id { get; set; }

    public double Battery { get; set; }

    public double? Longitude { get; set; }

    public double? Latitude { get; set; }

    public DateTime DateTime { get; set; }
}
