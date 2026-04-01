namespace DeviceManager.Server.Core.Services;

using System.Globalization;
using DeviceManager.Server.Core.Database;
using DeviceManager.Shared.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Stores and retrieves log entries sent from devices.
/// </summary>
public sealed class LogService(DbConnectionFactory dbFactory, ILogger<LogService> logger)
{
    public async Task AddLogEntryAsync(LogEntry entry)
    {
        using var connection = dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO DeviceLog (DeviceId, Level, Category, Message, Exception, Timestamp)
            VALUES (@DeviceId, @Level, @Category, @Message, @Exception, @Timestamp)
            """;
        command.Parameters.AddWithValue("@DeviceId", entry.DeviceId);
        command.Parameters.AddWithValue("@Level", (int)entry.Level);
        command.Parameters.AddWithValue("@Category", entry.Category);
        command.Parameters.AddWithValue("@Message", entry.Message);
        command.Parameters.AddWithValue("@Exception", (object?)entry.Exception ?? DBNull.Value);
        command.Parameters.AddWithValue("@Timestamp", FormatDateTime(entry.Timestamp));
        await command.ExecuteNonQueryAsync();
    }

    public async Task AddLogEntriesAsync(IEnumerable<LogEntry> entries)
    {
        using var connection = dbFactory.GetCommonConnection();
        using var transaction = connection.BeginTransaction();

        foreach (var entry in entries)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO DeviceLog (DeviceId, Level, Category, Message, Exception, Timestamp)
                VALUES (@DeviceId, @Level, @Category, @Message, @Exception, @Timestamp)
                """;
            command.Parameters.AddWithValue("@DeviceId", entry.DeviceId);
            command.Parameters.AddWithValue("@Level", (int)entry.Level);
            command.Parameters.AddWithValue("@Category", entry.Category);
            command.Parameters.AddWithValue("@Message", entry.Message);
            command.Parameters.AddWithValue("@Exception", (object?)entry.Exception ?? DBNull.Value);
            command.Parameters.AddWithValue("@Timestamp", FormatDateTime(entry.Timestamp));
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
        logger.LogDebug("Batch inserted log entries");
    }

    public async Task<List<LogEntry>> GetLogsAsync(string? deviceId, int? level, int skip, int take)
    {
        using var connection = dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();

        var sql = "SELECT LogId, DeviceId, Level, Category, Message, Exception, Timestamp FROM DeviceLog";
        var conditions = new List<string>();

        if (deviceId is not null)
        {
            conditions.Add("DeviceId = @DeviceId");
            command.Parameters.AddWithValue("@DeviceId", deviceId);
        }

        if (level.HasValue)
        {
            conditions.Add("Level >= @Level");
            command.Parameters.AddWithValue("@Level", level.Value);
        }

        if (conditions.Count > 0)
        {
            sql += " WHERE " + string.Join(" AND ", conditions);
        }

        sql += " ORDER BY Timestamp DESC LIMIT @Take OFFSET @Skip";
        command.Parameters.AddWithValue("@Take", take);
        command.Parameters.AddWithValue("@Skip", skip);
        command.CommandText = sql;

        var logs = new List<LogEntry>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            logs.Add(new LogEntry
            {
                LogId = reader.GetInt64(0),
                DeviceId = reader.GetString(1),
                Level = (Shared.Models.LogLevel)reader.GetInt32(2),
                Category = reader.GetString(3),
                Message = reader.GetString(4),
                Exception = reader.IsDBNull(5) ? null : reader.GetString(5),
                Timestamp = ParseDateTime(reader.GetString(6))
            });
        }

        return logs;
    }

    private static string FormatDateTime(DateTime dt) => dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    private static DateTime ParseDateTime(string s) => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
