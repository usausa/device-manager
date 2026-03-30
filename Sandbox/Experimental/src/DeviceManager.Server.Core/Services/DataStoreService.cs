using System.Globalization;
using DeviceManager.Server.Core.Database;
using DeviceManager.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DeviceManager.Server.Core.Services;

public sealed class DataStoreService
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly ILogger<DataStoreService> _logger;

    public DataStoreService(DbConnectionFactory dbFactory, ILogger<DataStoreService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<DataStoreEntry>> GetCommonEntriesAsync()
    {
        using var connection = _dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Key, Value, CreatedAt, UpdatedAt FROM CommonDataStore ORDER BY Key";

        var entries = new List<DataStoreEntry>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new DataStoreEntry
            {
                Key = reader.GetString(0),
                Value = reader.GetString(1),
                CreatedAt = ParseDateTime(reader.GetString(2)),
                UpdatedAt = ParseDateTime(reader.GetString(3))
            });
        }

        return entries;
    }

    public async Task<DataStoreEntry?> GetCommonEntryAsync(string key)
    {
        using var connection = _dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Key, Value, CreatedAt, UpdatedAt FROM CommonDataStore WHERE Key = @Key";
        command.Parameters.AddWithValue("@Key", key);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new DataStoreEntry
        {
            Key = reader.GetString(0),
            Value = reader.GetString(1),
            CreatedAt = ParseDateTime(reader.GetString(2)),
            UpdatedAt = ParseDateTime(reader.GetString(3))
        };
    }

    public async Task SetCommonEntryAsync(string key, string value)
    {
        var now = FormatDateTime(DateTime.UtcNow);
        using var connection = _dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO CommonDataStore (Key, Value, CreatedAt, UpdatedAt)
            VALUES (@Key, @Value, @Now, @Now)
            ON CONFLICT(Key) DO UPDATE SET
                Value = @Value,
                UpdatedAt = @Now
            """;
        command.Parameters.AddWithValue("@Key", key);
        command.Parameters.AddWithValue("@Value", value);
        command.Parameters.AddWithValue("@Now", now);
        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("Common data store entry set: {Key}", key);
    }

    public async Task DeleteCommonEntryAsync(string key)
    {
        using var connection = _dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM CommonDataStore WHERE Key = @Key";
        command.Parameters.AddWithValue("@Key", key);
        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("Common data store entry deleted: {Key}", key);
    }

    public async Task<List<DataStoreEntry>> GetDeviceEntriesAsync(string deviceId)
    {
        using var connection = _dbFactory.GetDeviceConnection(deviceId);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Key, Value, CreatedAt, UpdatedAt FROM DeviceDataStore ORDER BY Key";

        var entries = new List<DataStoreEntry>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new DataStoreEntry
            {
                Key = reader.GetString(0),
                Value = reader.GetString(1),
                CreatedAt = ParseDateTime(reader.GetString(2)),
                UpdatedAt = ParseDateTime(reader.GetString(3))
            });
        }

        return entries;
    }

    public async Task<DataStoreEntry?> GetDeviceEntryAsync(string deviceId, string key)
    {
        using var connection = _dbFactory.GetDeviceConnection(deviceId);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Key, Value, CreatedAt, UpdatedAt FROM DeviceDataStore WHERE Key = @Key";
        command.Parameters.AddWithValue("@Key", key);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return new DataStoreEntry
        {
            Key = reader.GetString(0),
            Value = reader.GetString(1),
            CreatedAt = ParseDateTime(reader.GetString(2)),
            UpdatedAt = ParseDateTime(reader.GetString(3))
        };
    }

    public async Task SetDeviceEntryAsync(string deviceId, string key, string value)
    {
        var now = FormatDateTime(DateTime.UtcNow);
        using var connection = _dbFactory.GetDeviceConnection(deviceId);
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO DeviceDataStore (Key, Value, CreatedAt, UpdatedAt)
            VALUES (@Key, @Value, @Now, @Now)
            ON CONFLICT(Key) DO UPDATE SET
                Value = @Value,
                UpdatedAt = @Now
            """;
        command.Parameters.AddWithValue("@Key", key);
        command.Parameters.AddWithValue("@Value", value);
        command.Parameters.AddWithValue("@Now", now);
        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("Device data store entry set: {DeviceId}/{Key}", deviceId, key);
    }

    public async Task DeleteDeviceEntryAsync(string deviceId, string key)
    {
        using var connection = _dbFactory.GetDeviceConnection(deviceId);
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM DeviceDataStore WHERE Key = @Key";
        command.Parameters.AddWithValue("@Key", key);
        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("Device data store entry deleted: {DeviceId}/{Key}", deviceId, key);
    }

    private static string FormatDateTime(DateTime dt) => dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    private static DateTime ParseDateTime(string s) => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
