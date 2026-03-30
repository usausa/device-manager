using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DeviceManager.Server.Core.Database;

public static class DatabaseInitializer
{
    private const string CommonSchema = """
        CREATE TABLE IF NOT EXISTS Device (
            DeviceId TEXT PRIMARY KEY,
            Name TEXT NOT NULL,
            Platform TEXT,
            "Group" TEXT,
            Tags TEXT,
            Status INTEGER NOT NULL DEFAULT 0,
            IsEnabled INTEGER NOT NULL DEFAULT 1,
            Note TEXT,
            RegisteredAt TEXT NOT NULL,
            LastConnectedAt TEXT,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS DeviceStatus (
            DeviceId TEXT PRIMARY KEY REFERENCES Device(DeviceId),
            Level INTEGER NOT NULL DEFAULT 0,
            Progress REAL NOT NULL DEFAULT 0,
            Battery INTEGER,
            Latitude REAL,
            Longitude REAL,
            CustomData TEXT,
            Timestamp TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS CommonConfig (
            Key TEXT PRIMARY KEY,
            Value TEXT NOT NULL,
            ValueType TEXT NOT NULL DEFAULT 'string',
            Description TEXT,
            UpdatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS CommonDataStore (
            Key TEXT PRIMARY KEY,
            Value TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Message (
            MessageId INTEGER PRIMARY KEY AUTOINCREMENT,
            DeviceId TEXT,
            Direction INTEGER NOT NULL,
            MessageType TEXT NOT NULL,
            Content TEXT NOT NULL,
            Status INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS ConfigHistory (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Scope TEXT NOT NULL,
            Key TEXT NOT NULL,
            OldValue TEXT,
            NewValue TEXT,
            ChangedAt TEXT NOT NULL
        );
        """;

    private const string DeviceSchema = """
        CREATE TABLE IF NOT EXISTS DeviceConfig (
            Key TEXT PRIMARY KEY,
            Value TEXT NOT NULL,
            ValueType TEXT NOT NULL DEFAULT 'string',
            UpdatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS DeviceDataStore (
            Key TEXT PRIMARY KEY,
            Value TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS StatusHistory (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Level INTEGER NOT NULL DEFAULT 0,
            Progress REAL NOT NULL DEFAULT 0,
            Battery INTEGER,
            Latitude REAL,
            Longitude REAL,
            CustomData TEXT,
            Timestamp TEXT NOT NULL
        );
        """;

    public static void InitializeCommonDatabase(string connectionString, ILogger? logger = null)
    {
        logger?.LogInformation("Initializing common database");

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = CommonSchema;
        command.ExecuteNonQuery();

        logger?.LogInformation("Common database initialized successfully");
    }

    public static void InitializeDeviceDatabase(string connectionString, ILogger? logger = null)
    {
        logger?.LogInformation("Initializing device database: {ConnectionString}", connectionString);

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = DeviceSchema;
        command.ExecuteNonQuery();

        logger?.LogInformation("Device database initialized successfully");
    }
}
