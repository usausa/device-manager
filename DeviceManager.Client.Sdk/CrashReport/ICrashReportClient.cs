namespace DeviceManager.Client.Sdk.CrashReport;

using DeviceManager.Shared.Models;

/// <summary>Sends crash reports from the device to the server.</summary>
public interface ICrashReportClient
{
    /// <summary>Sends a crash report to the server.</summary>
    Task SendAsync(CrashReport report, CancellationToken cancellationToken = default);
}
