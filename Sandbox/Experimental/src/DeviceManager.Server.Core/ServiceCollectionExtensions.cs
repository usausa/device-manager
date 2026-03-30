using DeviceManager.Server.Core.Database;
using DeviceManager.Server.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeviceManager.Server.Core;

public static class ServiceCollectionExtensions
{
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

        services.AddScoped<DeviceService>();
        services.AddScoped<ConfigService>();
        services.AddScoped<DataStoreService>();
        services.AddScoped<MessageService>();

        return services;
    }
}
