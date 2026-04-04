namespace DeviceManager.Server.Core.Database;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

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
            WifiRssi INTEGER,
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

        CREATE TABLE IF NOT EXISTS DeviceLog (
            LogId INTEGER PRIMARY KEY AUTOINCREMENT,
            DeviceId TEXT NOT NULL,
            Level INTEGER NOT NULL,
            Category TEXT NOT NULL,
            Message TEXT NOT NULL,
            Exception TEXT,
            Timestamp TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_DeviceLog_DeviceId ON DeviceLog(DeviceId);
        CREATE INDEX IF NOT EXISTS IX_DeviceLog_Timestamp ON DeviceLog(Timestamp);

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

        MigrateCommonDatabase(connection);

        logger?.LogInformation("Common database initialized successfully");
    }

    private static void MigrateCommonDatabase(SqliteConnection connection)
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA table_info(DeviceStatus)";
        using var reader = pragma.ExecuteReader();
        while (reader.Read())
        {
            existing.Add(reader.GetString(1));
        }

        if (!existing.Contains("WifiRssi"))
        {
            using var alter = connection.CreateCommand();
            alter.CommandText = "ALTER TABLE DeviceStatus ADD COLUMN WifiRssi INTEGER";
            alter.ExecuteNonQuery();

            // Backfill sample WiFi values for seeded rows that lack them
            using var backfill = connection.CreateCommand();
            backfill.CommandText = """
                UPDATE DeviceStatus SET WifiRssi = CASE DeviceId
                    WHEN 'dev-001'        THEN -52
                    WHEN 'dev-002'        THEN -68
                    WHEN 'tab-a'          THEN -45
                    WHEN 'tab-b'          THEN -78
                    WHEN 'kiosk-tky-01'   THEN -55
                    WHEN 'kiosk-tky-02'   THEN -82
                    WHEN 'sensor-hq-01'   THEN -41
                    WHEN 'laptop-field-01' THEN -60
                    WHEN 'rpi-lab-01'     THEN -75
                    ELSE NULL
                END
                WHERE WifiRssi IS NULL
                """;
            backfill.ExecuteNonQuery();
        }
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
