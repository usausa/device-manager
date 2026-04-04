namespace DeviceManager.Server.Core.Database;

using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

public static class DataSeeder
{
    public static void SeedCommonDatabase(DbConnectionFactory dbFactory, ILogger? logger = null)
    {
        using var connection = dbFactory.GetCommonConnection();

        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM Device;";
        var count = Convert.ToInt64(checkCmd.ExecuteScalar());

        if (count > 0)
        {
            logger?.LogInformation("Common database already contains data, skipping seed");
            return;
        }

        logger?.LogInformation("Seeding common database with sample data");

        using var transaction = connection.BeginTransaction();
        try
        {
            SeedDevices(connection);
            SeedDeviceStatuses(connection);
            SeedCommonConfig(connection);
            SeedCommonDataStore(connection);

            transaction.Commit();
            logger?.LogInformation("Common database seeded successfully");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            logger?.LogError(ex, "Failed to seed common database");
            throw;
        }
    }

    private static string FormatDateTime(DateTime dt) =>
        dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    private static void SeedDevices(SqliteConnection connection)
    {
        var now = DateTime.UtcNow;

        var devices = new[]
        {
            ("dev-001", "Device-001", "Android", "Warehouse", "scanner,mobile", 0, 1, "Main warehouse scanner", -30),
            ("dev-002", "Device-002", "Android", "Warehouse", "scanner,mobile", 0, 1, "Secondary warehouse scanner", -25),
            ("tab-a", "Tablet-A", "iOS", "Sales", "tablet,pos", 0, 1, "Sales floor tablet", -10),
            ("tab-b", "Tablet-B", "iOS", "Sales", "tablet,pos", 1, 1, "Back office tablet", -60),
            ("kiosk-tky-01", "Kiosk-Tokyo-01", "Windows", "Retail-JP", "kiosk,display", 0, 1, "Tokyo store kiosk", -5),
            ("kiosk-tky-02", "Kiosk-Tokyo-02", "Windows", "Retail-JP", "kiosk,display", 2, 1, "Tokyo store kiosk #2 - high temp", -3),
            ("sensor-hq-01", "Sensor-HQ-01", "Linux", "HQ", "sensor,iot", 0, 1, "Headquarters environment sensor", -2),
            ("sensor-hq-02", "Sensor-HQ-02", "Linux", "HQ", "sensor,iot", 3, 0, "Faulty sensor - pending replacement", -120),
            ("laptop-field-01", "Laptop-Field-01", "Windows", "Field", "laptop,mobile", 0, 1, "Field technician laptop", -15),
            ("rpi-lab-01", "RPi-Lab-01", "Linux", "R&D", "raspberry-pi,prototype", 2, 1, "Lab prototype unit - unstable firmware", -8),
        };

        const string sql = """
            INSERT INTO Device (DeviceId, Name, Platform, "Group", Tags, Status, IsEnabled, Note, RegisteredAt, LastConnectedAt, CreatedAt, UpdatedAt)
            VALUES (@DeviceId, @Name, @Platform, @Group, @Tags, @Status, @IsEnabled, @Note, @RegisteredAt, @LastConnectedAt, @CreatedAt, @UpdatedAt);
            """;

        foreach (var (id, name, platform, group, tags, status, isEnabled, note, lastConnectedMinutesAgo) in devices)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@DeviceId", id);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Platform", platform);
            cmd.Parameters.AddWithValue("@Group", group);
            cmd.Parameters.AddWithValue("@Tags", tags);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@IsEnabled", isEnabled);
            cmd.Parameters.AddWithValue("@Note", note);
            cmd.Parameters.AddWithValue("@RegisteredAt", FormatDateTime(now.AddDays(-90)));
            cmd.Parameters.AddWithValue("@LastConnectedAt", FormatDateTime(now.AddMinutes(lastConnectedMinutesAgo)));
            cmd.Parameters.AddWithValue("@CreatedAt", FormatDateTime(now.AddDays(-90)));
            cmd.Parameters.AddWithValue("@UpdatedAt", FormatDateTime(now));
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedDeviceStatuses(SqliteConnection connection)
    {
        var now = DateTime.UtcNow;

        var statuses = new[]
        {
            ("dev-001",         0, 85.0,  72,         (int?)-52, 35.6812,  139.7671,  "{\"scanCount\":1423}"),
            ("dev-002",         0, 62.5,  58,         (int?)-68, 35.6815,  139.7675,  "{\"scanCount\":987}"),
            ("tab-a",           0, 100.0, 91,         (int?)-45, 34.0522, -118.2437,  "{\"activeSession\":true}"),
            ("tab-b",           1, 0.0,   34,         (int?)-78, 34.0525, -118.2440,  "{\"activeSession\":false}"),
            ("kiosk-tky-01",    0, 50.0,  (int?)null, (int?)-55, 35.6895,  139.6917,  "{\"displayOn\":true,\"uptimeHours\":148}"),
            ("kiosk-tky-02",    2, 48.0,  (int?)null, (int?)-82, 35.6897,  139.6920,  "{\"displayOn\":true,\"tempCelsius\":42}"),
            ("sensor-hq-01",    0, 100.0, 100,        (int?)-41, 37.7749, -122.4194,  "{\"humidity\":45,\"tempCelsius\":22}"),
            ("sensor-hq-02",    3, 0.0,   5,          (int?)null, 37.7750, -122.4195,  "{\"error\":\"SENSOR_FAULT\"}"),
            ("laptop-field-01", 0, 75.0,  63,         (int?)-60, 40.7128,  -74.0060,  "{\"vpnConnected\":true}"),
            ("rpi-lab-01",      2, 30.0,  (int?)null, (int?)-75, 37.3861, -122.0839,  "{\"firmwareVersion\":\"0.9.3-beta\",\"crashCount\":7}"),
        };

        const string sql = """
            INSERT INTO DeviceStatus (DeviceId, Level, Progress, Battery, WifiRssi, Latitude, Longitude, CustomData, Timestamp)
            VALUES (@DeviceId, @Level, @Progress, @Battery, @WifiRssi, @Latitude, @Longitude, @CustomData, @Timestamp);
            """;

        foreach (var (deviceId, level, progress, battery, wifiRssi, lat, lon, customData) in statuses)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@DeviceId", deviceId);
            cmd.Parameters.AddWithValue("@Level", level);
            cmd.Parameters.AddWithValue("@Progress", progress);
            cmd.Parameters.AddWithValue("@Battery", battery.HasValue ? (object)battery.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@WifiRssi", wifiRssi.HasValue ? (object)wifiRssi.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@Latitude", lat);
            cmd.Parameters.AddWithValue("@Longitude", lon);
            cmd.Parameters.AddWithValue("@CustomData", customData);
            cmd.Parameters.AddWithValue("@Timestamp", FormatDateTime(now));
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedCommonConfig(SqliteConnection connection)
    {
        var now = DateTime.UtcNow;

        var configs = new[]
        {
            ("app.version", "1.0.0", "string", "Application version"),
            ("sync.interval", "30", "int", "Device sync interval in seconds"),
            ("log.level", "Information", "string", "Default logging level"),
            ("max.devices", "100", "int", "Maximum number of managed devices"),
            ("heartbeat.timeout", "120", "int", "Heartbeat timeout in seconds"),
            ("notification.enabled", "true", "bool", "Enable push notifications for alerts"),
            ("data.retention.days", "90", "int", "Days to retain historical data"),
        };

        const string sql = """
            INSERT INTO CommonConfig (Key, Value, ValueType, Description, UpdatedAt)
            VALUES (@Key, @Value, @ValueType, @Description, @UpdatedAt);
            """;

        foreach (var (key, value, valueType, description) in configs)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@Key", key);
            cmd.Parameters.AddWithValue("@Value", value);
            cmd.Parameters.AddWithValue("@ValueType", valueType);
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.Parameters.AddWithValue("@UpdatedAt", FormatDateTime(now));
            cmd.ExecuteNonQuery();
        }
    }

    private static void SeedCommonDataStore(SqliteConnection connection)
    {
        var now = DateTime.UtcNow;

        var entries = new[]
        {
            ("dashboard.layout", "{\"columns\":3,\"theme\":\"dark\",\"refreshRate\":10}"),
            ("alert.rules", "[{\"metric\":\"battery\",\"op\":\"lt\",\"value\":20,\"severity\":\"warning\"}]"),
            ("device.groups.order", "[\"Warehouse\",\"Sales\",\"Retail-JP\",\"HQ\",\"Field\",\"R&D\"]"),
        };

        const string sql = """
            INSERT INTO CommonDataStore (Key, Value, CreatedAt, UpdatedAt)
            VALUES (@Key, @Value, @CreatedAt, @UpdatedAt);
            """;

        foreach (var (key, value) in entries)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@Key", key);
            cmd.Parameters.AddWithValue("@Value", value);
            cmd.Parameters.AddWithValue("@CreatedAt", FormatDateTime(now));
            cmd.Parameters.AddWithValue("@UpdatedAt", FormatDateTime(now));
            cmd.ExecuteNonQuery();
        }
    }
}
