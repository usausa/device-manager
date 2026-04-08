namespace DeviceManager.Client.Sdk.CrashReport;

using DeviceManager.Client.Sdk.Connection;
using DeviceManager.Shared;
using DeviceManager.Shared.Models;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

internal sealed class CrashReportClient : ICrashReportClient
{
    private readonly SignalRConnectionManager? signalRConnectionManager;
    private readonly GrpcConnectionManager? grpcConnectionManager;
    private readonly ILogger logger;

    public CrashReportClient(
        SignalRConnectionManager? signalRConnectionManager,
        GrpcConnectionManager? grpcConnectionManager,
        ILogger logger)
    {
        this.signalRConnectionManager = signalRConnectionManager;
        this.grpcConnectionManager = grpcConnectionManager;
        this.logger = logger;
    }

    public async Task SendAsync(CrashReport report, CancellationToken cancellationToken = default)
    {
        if (grpcConnectionManager is not null)
        {
            await grpcConnectionManager.SendCrashReportAsync(report, cancellationToken).ConfigureAwait(false);
            return;
        }

        var hub = signalRConnectionManager?.HubConnection
            ?? throw new InvalidOperationException("Not connected to the server.");

        logger.LogInformation("Sending crash report: {ExceptionType}", report.ExceptionType);
        await hub.InvokeAsync(HubConstants.ClientMethods.SendCrashReport, report, cancellationToken)
            .ConfigureAwait(false);
    }
}
