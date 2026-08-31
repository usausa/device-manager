namespace DeviceManager.Client;

using DeviceManager.Client.Logging;
using DeviceManager.Client.Telemetry;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    // クライアントの登録(IDeviceInfoProvider は利用側で登録する。IDeviceStatusProvider は任意)
    public static IServiceCollection AddDeviceManagerClient(this IServiceCollection services, Action<DeviceManagerClientOptions> configure)
    {
        var options = new DeviceManagerClientOptions();
        configure(options);

        services.AddSingleton(options);
        // テレメトリの内部ログはロギング基盤との循環を避けるため既定(null)とする
        services.AddSingleton(static p => new DeviceManagerTelemetry(
            p.GetRequiredService<DeviceManagerClientOptions>(),
            p.GetRequiredService<IDeviceInfoProvider>()));
        services.AddSingleton(static p => new DeviceManagerClient(
            p.GetRequiredService<DeviceManagerClientOptions>(),
            p.GetRequiredService<IDeviceInfoProvider>(),
            p.GetRequiredService<DeviceManagerTelemetry>(),
            p.GetRequiredService<ILogger<DeviceManagerClient>>(),
            p.GetService<IDeviceStatusProvider>()));

        return services;
    }

    // 端末ログ転送(Microsoft.Extensions.Logging ベース)の登録
    public static IServiceCollection AddDeviceManagerLogging(this IServiceCollection services)
    {
        services.AddSingleton<ILoggerProvider>(static p => new DeviceManagerLoggerProvider(
            p.GetRequiredService<DeviceManagerClientOptions>(),
            p.GetRequiredService<DeviceManagerTelemetry>()));
        return services;
    }
}
