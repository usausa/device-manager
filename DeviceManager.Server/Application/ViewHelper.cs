namespace DeviceManager.Server.Application;

using MudBlazor;

// 画面表示用のヘルパ(アイコン・色・書式)
public static class ViewHelper
{
    //--------------------------------------------------------------------------------
    // State
    //--------------------------------------------------------------------------------

    public static Color StateColor(int status) => (DeviceState)status switch
    {
        DeviceState.Active => Color.Success,
        DeviceState.Warning => Color.Warning,
        DeviceState.Error => Color.Error,
        _ => Color.Default
    };

    public static string StateText(int status) => (DeviceState)status switch
    {
        DeviceState.Active => "稼働",
        DeviceState.Warning => "警告",
        DeviceState.Error => "異常",
        _ => "停止"
    };

    //--------------------------------------------------------------------------------
    // Wifi
    //--------------------------------------------------------------------------------

    public static string WifiIcon(int? rssi) => rssi switch
    {
        null => Icons.Material.Filled.SignalWifiOff,
        >= -67 => Icons.Material.Filled.SignalWifi4Bar,
        >= -75 => Icons.Material.Filled.NetworkWifi,
        _ => Icons.Material.Filled.SignalWifi0Bar
    };

    public static Color WifiColor(int? rssi) => rssi switch
    {
        null => Color.Default,
        >= -67 => Color.Success,
        >= -75 => Color.Success,
        >= -85 => Color.Warning,
        _ => Color.Error
    };

    //--------------------------------------------------------------------------------
    // Battery
    //--------------------------------------------------------------------------------

    public static string BatteryIcon(double? battery) => battery switch
    {
        null => Icons.Material.Filled.BatteryUnknown,
        >= 95 => Icons.Material.Filled.BatteryFull,
        >= 85 => Icons.Material.Filled.Battery6Bar,
        >= 70 => Icons.Material.Filled.Battery5Bar,
        >= 55 => Icons.Material.Filled.Battery4Bar,
        >= 40 => Icons.Material.Filled.Battery3Bar,
        >= 25 => Icons.Material.Filled.Battery2Bar,
        >= 10 => Icons.Material.Filled.Battery1Bar,
        _ => Icons.Material.Filled.BatteryAlert
    };

    public static Color BatteryColor(double? battery) => battery switch
    {
        null => Color.Default,
        >= 50 => Color.Success,
        >= 20 => Color.Warning,
        _ => Color.Error
    };

    //--------------------------------------------------------------------------------
    // Log
    //--------------------------------------------------------------------------------

    public static Color LogLevelColor(int level) => (DeviceLogLevel)level switch
    {
        DeviceLogLevel.Critical => Color.Error,
        DeviceLogLevel.Error => Color.Error,
        DeviceLogLevel.Warning => Color.Warning,
        DeviceLogLevel.Information => Color.Info,
        _ => Color.Default
    };

    public static string LogLevelText(int level) => (DeviceLogLevel)level switch
    {
        DeviceLogLevel.Critical => "Critical",
        DeviceLogLevel.Error => "Error",
        DeviceLogLevel.Warning => "Warning",
        DeviceLogLevel.Information => "Info",
        DeviceLogLevel.Debug => "Debug",
        _ => "Trace"
    };

    //--------------------------------------------------------------------------------
    // Message
    //--------------------------------------------------------------------------------

    public static string MessageDirectionIcon(int direction) => (MessageDirection)direction == MessageDirection.ServerToDevice
        ? Icons.Material.Filled.CallMade
        : Icons.Material.Filled.CallReceived;

    public static Color MessageStatusColor(int status) => (MessageDeliveryStatus)status switch
    {
        MessageDeliveryStatus.Delivered => Color.Success,
        MessageDeliveryStatus.Failed => Color.Error,
        _ => Color.Info
    };

    public static string MessageStatusText(int status) => (MessageDeliveryStatus)status switch
    {
        MessageDeliveryStatus.Delivered => "配送済",
        MessageDeliveryStatus.Failed => "失敗",
        _ => "送信済"
    };

    //--------------------------------------------------------------------------------
    // Format
    //--------------------------------------------------------------------------------

    public static string FormatSize(long size) => size switch
    {
        >= 1_099_511_627_776 => $"{size / 1_099_511_627_776d:F2} TB",
        >= 1_073_741_824 => $"{size / 1_073_741_824d:F1} GB",
        >= 1_048_576 => $"{size / 1_048_576d:F1} MB",
        >= 1_024 => $"{size / 1_024d:F1} KB",
        _ => $"{size} B"
    };

    // 相対時刻表示(たった今 / n分前 / n時間前 / n日前 / 日付)
    public static string RelativeTime(DateTime? value, DateTime now)
    {
        if (value is null)
        {
            return "-";
        }

        var diff = now - value.Value;
        return diff switch
        {
            { TotalMinutes: < 1 } => "たった今",
            { TotalHours: < 1 } => $"{(int)diff.TotalMinutes}分前",
            { TotalDays: < 1 } => $"{(int)diff.TotalHours}時間前",
            { TotalDays: < 7 } => $"{(int)diff.TotalDays}日前",
            _ => value.Value.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture)
        };
    }

    //--------------------------------------------------------------------------------
    // File
    //--------------------------------------------------------------------------------

    public static string FileIcon(bool directory, string name)
    {
        if (directory)
        {
            return Icons.Material.Filled.Folder;
        }

        return Path.GetExtension(name).ToUpperInvariant() switch
        {
            ".PNG" or ".JPG" or ".JPEG" or ".GIF" or ".BMP" or ".WEBP" or ".SVG" => Icons.Material.Filled.Image,
            ".MP4" or ".WEBM" or ".AVI" or ".MOV" => Icons.Material.Filled.Movie,
            ".MP3" or ".WAV" or ".OGG" => Icons.Material.Filled.MusicNote,
            ".PDF" => Icons.Material.Filled.PictureAsPdf,
            ".ZIP" or ".RAR" or ".7Z" or ".TAR" or ".GZ" => Icons.Material.Filled.Archive,
            ".TXT" or ".LOG" or ".MD" or ".CSV" => Icons.Material.Filled.Description,
            ".CS" or ".JS" or ".TS" or ".PY" or ".HTML" or ".CSS" or ".JSON" or ".XML" => Icons.Material.Filled.Code,
            _ => Icons.Material.Filled.InsertDriveFile
        };
    }

    public static Color FileIconColor(bool directory) =>
        directory ? Color.Warning : Color.Default;
}
