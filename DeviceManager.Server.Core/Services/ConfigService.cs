namespace DeviceManager.Server.Core.Services;

using System.Globalization;
using DeviceManager.Server.Core.Database;
using DeviceManager.Shared.Models;
using Microsoft.Extensions.Logging;

public sealed class ConfigService(DbConnectionFactory dbFactory, ILogger<ConfigService> logger)
{
    public async Task<List<ConfigEntry>> GetCommonConfigAsync()
    {
        using var connection = dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Key, Value, ValueType, Description FROM CommonConfig ORDER BY Key";

        var entries = new List<ConfigEntry>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new ConfigEntry
            {
                Key = reader.GetString(0),
                Value = reader.GetString(1),
                ValueType = reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        return entries;
    }

    public async Task SetCommonConfigAsync(string key, ConfigEntry entry)
    {
        using var connection = dbFactory.GetCommonConnection();
        var now = FormatDateTime(DateTime.UtcNow);

        string? oldValue = null;
        using (var getCmd = connection.CreateCommand())
        {
            getCmd.CommandText = "SELECT Value FROM CommonConfig WHERE Key = @Key";
            getCmd.Parameters.AddWithValue("@Key", key);
            var result = await getCmd.ExecuteScalarAsync();
            oldValue = result as string;
        }

        using (var upsertCmd = connection.CreateCommand())
        {
            upsertCmd.CommandText = """
                INSERT INTO CommonConfig (Key, Value, ValueType, Description, UpdatedAt)
                VALUES (@Key, @Value, @ValueType, @Description, @UpdatedAt)
                ON CONFLICT(Key) DO UPDATE SET
                    Value = @Value, ValueType = @ValueType, Description = @Description, UpdatedAt = @UpdatedAt
                """;
            upsertCmd.Parameters.AddWithValue("@Key", key);
            upsertCmd.Parameters.AddWithValue("@Value", entry.Value);
            upsertCmd.Parameters.AddWithValue("@ValueType", entry.ValueType);
            upsertCmd.Parameters.AddWithValue("@Description", (object?)entry.Description ?? DBNull.Value);
            upsertCmd.Parameters.AddWithValue("@UpdatedAt", now);
            await upsertCmd.ExecuteNonQueryAsync();
        }

        using (var historyCmd = connection.CreateCommand())
        {
            historyCmd.CommandText = """
                INSERT INTO ConfigHistory (Scope, Key, OldValue, NewValue, ChangedAt)
                VALUES (@Scope, @Key, @OldValue, @NewValue, @ChangedAt)
                """;
            historyCmd.Parameters.AddWithValue("@Scope", "common");
            historyCmd.Parameters.AddWithValue("@Key", key);
            historyCmd.Parameters.AddWithValue("@OldValue", (object?)oldValue ?? DBNull.Value);
            historyCmd.Parameters.AddWithValue("@NewValue", entry.Value);
            historyCmd.Parameters.AddWithValue("@ChangedAt", now);
            await historyCmd.ExecuteNonQueryAsync();
        }

        logger.LogInformation("Common config set: {Key}", key);
    }

    public async Task DeleteCommonConfigAsync(string key)
    {
        using var connection = dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM CommonConfig WHERE Key = @Key";
        command.Parameters.AddWithValue("@Key", key);
        await command.ExecuteNonQueryAsync();
        logger.LogInformation("Common config deleted: {Key}", key);
    }

    public async Task<List<ConfigEntry>> GetDeviceConfigAsync(string deviceId)
    {
        using var connection = dbFactory.GetDeviceConnection(deviceId);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Key, Value, ValueType FROM DeviceConfig ORDER BY Key";

        var entries = new List<ConfigEntry>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new ConfigEntry
            {
                Key = reader.GetString(0),
                Value = reader.GetString(1),
                ValueType = reader.GetString(2)
            });
        }

        return entries;
    }

    public async Task SetDeviceConfigAsync(string deviceId, string key, ConfigEntry entry)
    {
        var now = FormatDateTime(DateTime.UtcNow);

        string? oldValue = null;
        using (var deviceConn = dbFactory.GetDeviceConnection(deviceId))
        using (var getCmd = deviceConn.CreateCommand())
        {
            getCmd.CommandText = "SELECT Value FROM DeviceConfig WHERE Key = @Key";
            getCmd.Parameters.AddWithValue("@Key", key);
            var result = await getCmd.ExecuteScalarAsync();
            oldValue = result as string;
        }

        using (var deviceConn = dbFactory.GetDeviceConnection(deviceId))
        using (var upsertCmd = deviceConn.CreateCommand())
        {
            upsertCmd.CommandText = """
                INSERT INTO DeviceConfig (Key, Value, ValueType, UpdatedAt)
                VALUES (@Key, @Value, @ValueType, @UpdatedAt)
                ON CONFLICT(Key) DO UPDATE SET Value = @Value, ValueType = @ValueType, UpdatedAt = @UpdatedAt
                """;
            upsertCmd.Parameters.AddWithValue("@Key", key);
            upsertCmd.Parameters.AddWithValue("@Value", entry.Value);
            upsertCmd.Parameters.AddWithValue("@ValueType", entry.ValueType);
            upsertCmd.Parameters.AddWithValue("@UpdatedAt", now);
            await upsertCmd.ExecuteNonQueryAsync();
        }

        using (var commonConn = dbFactory.GetCommonConnection())
        using (var historyCmd = commonConn.CreateCommand())
        {
            historyCmd.CommandText = """
                INSERT INTO ConfigHistory (Scope, Key, OldValue, NewValue, ChangedAt)
                VALUES (@Scope, @Key, @OldValue, @NewValue, @ChangedAt)
                """;
            historyCmd.Parameters.AddWithValue("@Scope", $"device:{deviceId}");
            historyCmd.Parameters.AddWithValue("@Key", key);
            historyCmd.Parameters.AddWithValue("@OldValue", (object?)oldValue ?? DBNull.Value);
            historyCmd.Parameters.AddWithValue("@NewValue", entry.Value);
            historyCmd.Parameters.AddWithValue("@ChangedAt", now);
            await historyCmd.ExecuteNonQueryAsync();
        }

        logger.LogInformation("Device config set: {DeviceId}/{Key}", deviceId, key);
    }

    public async Task DeleteDeviceConfigAsync(string deviceId, string key)
    {
        using var connection = dbFactory.GetDeviceConnection(deviceId);
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM DeviceConfig WHERE Key = @Key";
        command.Parameters.AddWithValue("@Key", key);
        await command.ExecuteNonQueryAsync();
        logger.LogInformation("Device config deleted: {DeviceId}/{Key}", deviceId, key);
    }

    public async Task<List<ConfigEntry>> GetResolvedConfigAsync(string deviceId)
    {
        var commonEntries = await GetCommonConfigAsync();
        var resolved = new Dictionary<string, ConfigEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in commonEntries)
        {
            resolved[entry.Key] = entry;
        }

        var deviceEntries = await GetDeviceConfigAsync(deviceId);
        foreach (var entry in deviceEntries)
        {
            resolved[entry.Key] = entry;
        }

        return resolved.Values.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string FormatDateTime(DateTime dt) => dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
}
