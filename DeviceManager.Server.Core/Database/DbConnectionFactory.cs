namespace DeviceManager.Server.Core.Database;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

public sealed class DbConnectionFactory
{
    private readonly string dataDirectory;
    private readonly ILogger<DbConnectionFactory> logger;
    private readonly string commonConnectionString;

    public DbConnectionFactory(string dataDirectory, ILogger<DbConnectionFactory> logger)
    {
        this.dataDirectory = dataDirectory;
        this.logger = logger;

        Directory.CreateDirectory(dataDirectory);

        var commonDbPath = Path.Combine(dataDirectory, "Common.db");
        commonConnectionString = BuildConnectionString(commonDbPath);
    }

    public SqliteConnection GetCommonConnection()
    {
        var connection = new SqliteConnection(commonConnectionString);
        connection.Open();
        return connection;
    }

    public SqliteConnection GetDeviceConnection(string deviceId)
    {
        var deviceDbPath = Path.Combine(dataDirectory, $"Device_{deviceId}.db");
        var connString = BuildConnectionString(deviceDbPath);

        if (!File.Exists(deviceDbPath))
        {
            logger.LogInformation("Creating device database for {DeviceId}", deviceId);
            DatabaseInitializer.InitializeDeviceDatabase(connString, logger);
        }

        var connection = new SqliteConnection(connString);
        connection.Open();
        return connection;
    }

    public void InitializeCommonDatabase()
    {
        DatabaseInitializer.InitializeCommonDatabase(commonConnectionString, logger);
    }

    public void InitializeDeviceDatabase(string deviceId)
    {
        var deviceDbPath = Path.Combine(dataDirectory, $"Device_{deviceId}.db");
        var connString = BuildConnectionString(deviceDbPath);
        DatabaseInitializer.InitializeDeviceDatabase(connString, logger);
    }

    public void DeleteDeviceDatabase(string deviceId)
    {
        var deviceDbPath = Path.Combine(dataDirectory, $"Device_{deviceId}.db");
        if (File.Exists(deviceDbPath))
        {
            SqliteConnection.ClearPool(new SqliteConnection(BuildConnectionString(deviceDbPath)));
            File.Delete(deviceDbPath);
            logger.LogInformation("Deleted device database for {DeviceId}", deviceId);
        }
    }

    private static string BuildConnectionString(string dbPath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };
        return builder.ConnectionString;
    }
}
