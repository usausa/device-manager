namespace DeviceManager.Components.Pages;

using Microsoft.AspNetCore.Components;

public partial class Logs : IDisposable
{
    [Inject]
    public LogService LogService { get; set; } = default!;

    [Inject]
    public DeviceEventService EventBus { get; set; } = default!;

    private List<LogEntry> logs = [];
    private string filterDeviceId = string.Empty;
    private int? filterLevel;
    private bool isLoading;
    private IDisposable? logSub;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        logSub = EventBus.Subscribe(AppEvents.LogReceived, async _ =>
        {
            await LoadAsync();
            await InvokeAsync(StateHasChanged);
        });
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        var deviceId = string.IsNullOrWhiteSpace(filterDeviceId) ? null : filterDeviceId.Trim();
        logs = await LogService.GetLogsAsync(deviceId, filterLevel, 0, 500);
        isLoading = false;
    }

    private static Color GetLevelColor(Shared.Models.LogLevel level) => level switch
    {
        Shared.Models.LogLevel.Critical => Color.Error,
        Shared.Models.LogLevel.Error => Color.Error,
        Shared.Models.LogLevel.Warning => Color.Warning,
        Shared.Models.LogLevel.Information => Color.Info,
        Shared.Models.LogLevel.Debug => Color.Default,
        _ => Color.Default
    };

    public void Dispose() => logSub?.Dispose();
}
