namespace DeviceManager.Server.Core;

using DeviceManager.Server.Core.Database;
using DeviceManager.Server.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers DeviceManager core services (database, device, config, datastore, message, log).
    /// </summary>
    public static IServiceCollection AddDeviceManagerCore(this IServiceCollection services, string dataDirectory)
    {
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<DbConnectionFactory>>();
            var factory = new DbConnectionFactory(dataDirectory, logger);
            factory.InitializeCommonDatabase();
            DataSeeder.SeedCommonDatabase(factory, logger);
            return factory;
        });

        services.AddSingleton<DeviceService>();
        services.AddSingleton<ConfigService>();
        services.AddSingleton<DataStoreService>();
        services.AddSingleton<MessageService>();
        services.AddSingleton<LogService>();
        services.AddSingleton<CrashReportService>();

        return services;
    }
}
