using DeviceManager.Client.Sdk.Config;
using DeviceManager.Client.Sdk.DataStore;
using DeviceManager.Client.Sdk.Messaging;
using DeviceManager.Client.Sdk.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeviceManager.Client.Sdk;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeviceManagerClient(
        this IServiceCollection services,
        Action<DeviceManagerClientOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = new DeviceManagerClientOptions { ServerUrl = null! };
        configureOptions(options);

        if (string.IsNullOrWhiteSpace(options.ServerUrl))
        {
            throw new ArgumentException("ServerUrl must be configured.", nameof(configureOptions));
        }

        services.AddSingleton(options);

        services.AddHttpClient("DeviceManager", client =>
        {
            client.BaseAddress = new Uri(options.ServerUrl.TrimEnd('/') + "/");
            client.Timeout = options.ApiTimeout;
        });

        services.AddSingleton(sp =>
        {
            var deviceInfo = sp.GetRequiredService<IDeviceInfoProvider>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("DeviceManager");
            var statusProvider = sp.GetService<IDeviceStatusProvider>();
            var commandHandler = sp.GetService<IDeviceCommandHandler>();

            return new DeviceManagerClient(
                options, deviceInfo, loggerFactory, httpClient,
                statusProvider, commandHandler);
        });

        services.AddSingleton<IConfigManager>(sp => sp.GetRequiredService<DeviceManagerClient>().Config);
        services.AddSingleton<IDataStoreClient>(sp => sp.GetRequiredService<DeviceManagerClient>().DataStore);
        services.AddSingleton<IMessageClient>(sp => sp.GetRequiredService<DeviceManagerClient>().Messages);
        services.AddSingleton<IStorageClient>(sp => sp.GetRequiredService<DeviceManagerClient>().Storage);

        return services;
    }
}
