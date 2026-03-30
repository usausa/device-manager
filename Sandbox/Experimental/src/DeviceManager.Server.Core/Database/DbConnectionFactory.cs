using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DeviceManager.Server.Core.Database;

public sealed class DbConnectionFactory
{
    private readonly string _dataDirectory;
    private readonly ILogger<DbConnectionFactory> _logger;
    private readonly string _commonConnectionString;

    public DbConnectionFactory(string dataDirectory, ILogger<DbConnectionFactory> logger)
    {
        _dataDirectory = dataDirectory;
        _logger = logger;

        Directory.CreateDirectory(_dataDirectory);

        var commonDbPath = Path.Combine(_dataDirectory, "Common.db");
        _commonConnectionString = BuildConnectionString(commonDbPath);
    }

    public SqliteConnection GetCommonConnection()
    {
        var connection = new SqliteConnection(_commonConnectionString);
        connection.Open();
        return connection;
    }

    public SqliteConnection GetDeviceConnection(string deviceId)
    {
        var deviceDbPath = Path.Combine(_dataDirectory, $"Device_{deviceId}.db");
        var connectionString = BuildConnectionString(deviceDbPath);

        if (!File.Exists(deviceDbPath))
        {
            _logger.LogInformation("Creating device database for {DeviceId}", deviceId);
            DatabaseInitializer.InitializeDeviceDatabase(connectionString, _logger);
        }

        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    public void InitializeCommonDatabase()
    {
        DatabaseInitializer.InitializeCommonDatabase(_commonConnectionString, _logger);
    }

    public void InitializeDeviceDatabase(string deviceId)
    {
        var deviceDbPath = Path.Combine(_dataDirectory, $"Device_{deviceId}.db");
        var connectionString = BuildConnectionString(deviceDbPath);
        DatabaseInitializer.InitializeDeviceDatabase(connectionString, _logger);
    }

    public void DeleteDeviceDatabase(string deviceId)
    {
        var deviceDbPath = Path.Combine(_dataDirectory, $"Device_{deviceId}.db");
        if (File.Exists(deviceDbPath))
        {
            // Close any pooled connections before deleting
            SqliteConnection.ClearPool(new SqliteConnection(BuildConnectionString(deviceDbPath)));
            File.Delete(deviceDbPath);
            _logger.LogInformation("Deleted device database for {DeviceId}", deviceId);
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
