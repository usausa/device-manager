namespace DeviceManager.Models;

// ダッシュボードのサマリー値
public sealed record DashboardSummary(
    int Total,
    int ActiveDay,
    int ActiveWeek,
    int LowBattery,
    int Warning,
    int Error);
