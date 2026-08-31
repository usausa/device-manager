namespace DeviceManager.TestClient;

// ログ転送テスト用のログ定義
internal static partial class TestLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Test information log. value=[{value}]")]
    public static partial void InfoTest(this ILogger logger, int value);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Test warning log.")]
    public static partial void WarnTest(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Test error log.")]
    public static partial void ErrorTest(this ILogger logger, Exception ex);
}
