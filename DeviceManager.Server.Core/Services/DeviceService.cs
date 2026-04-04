namespace DeviceManager.Server.Core.Services;

using System.Globalization;
using System.Text.Json;
using DeviceManager.Server.Core.Database;
using DeviceManager.Shared.Models;
using Microsoft.Extensions.Logging;

public sealed class DeviceService(DbConnectionFactory dbFactory, ILogger<DeviceService> logger)
{
    public async Task<List<DeviceSummary>> GetAllDevicesAsync()
    {
        using var connection = dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT d.DeviceId, d.Name, d."Group", d.Status, d.LastConnectedAt,
                   ds.Level, ds.Progress, ds.Battery, ds.WifiRssi, ds.Timestamp
            FROM Device d
            LEFT JOIN DeviceStatus ds ON d.DeviceId = ds.DeviceId
            ORDER BY d.Name
            """;

        var devices = new List<DeviceSummary>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            devices.Add(new DeviceSummary
            {
                DeviceId = reader.GetString(0),
                Name = reader.GetString(1),
                Group = reader.IsDBNull(2) ? null : reader.GetString(2),
                Status = (DeviceConnectionStatus)reader.GetInt32(3),
                LastConnectedAt = reader.IsDBNull(4) ? null : ParseDateTime(reader.GetString(4)),
                Level = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                Progress = reader.IsDBNull(6) ? 0 : reader.GetDouble(6),
                Battery = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                WifiRssi = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                StatusTimestamp = reader.IsDBNull(9) ? null : ParseDateTime(reader.GetString(9))
            });
        }

        return devices;
    }

    public async Task<DeviceDetail?> GetDeviceAsync(string deviceId)
    {
        using var connection = dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT d.DeviceId, d.Name, d.Platform, d."Group", d.Tags, d.Note, d.Status,
                   d.IsEnabled, d.RegisteredAt, d.LastConnectedAt,
                   ds.Level, ds.Progress, ds.Battery, ds.WifiRssi, ds.Latitude, ds.Longitude,
                   ds.CustomData, ds.Timestamp
            FROM Device d
            LEFT JOIN DeviceStatus ds ON d.DeviceId = ds.DeviceId
            WHERE d.DeviceId = @DeviceId
            """;
        command.Parameters.AddWithValue("@DeviceId", deviceId);

        using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new DeviceDetail
        {
            DeviceId = reader.GetString(0),
            Name = reader.GetString(1),
            Platform = reader.IsDBNull(2) ? null : reader.GetString(2),
            Group = reader.IsDBNull(3) ? null : reader.GetString(3),
            Tags = reader.IsDBNull(4) ? null : ParseTags(reader.GetString(4)),
            Note = reader.IsDBNull(5) ? null : reader.GetString(5),
            Status = (DeviceConnectionStatus)reader.GetInt32(6),
            IsEnabled = reader.GetInt32(7) != 0,
            RegisteredAt = ParseDateTime(reader.GetString(8)),
            LastConnectedAt = reader.IsDBNull(9) ? null : ParseDateTime(reader.GetString(9)),
            Level = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
            Progress = reader.IsDBNull(11) ? 0 : reader.GetDouble(11),
            Battery = reader.IsDBNull(12) ? null : reader.GetInt32(12),
            WifiRssi = reader.IsDBNull(13) ? null : reader.GetInt32(13),
            Latitude = reader.IsDBNull(14) ? null : reader.GetDouble(14),
            Longitude = reader.IsDBNull(15) ? null : reader.GetDouble(15),
            CustomData = reader.IsDBNull(16) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(16)),
            StatusTimestamp = reader.IsDBNull(17) ? null : ParseDateTime(reader.GetString(17))
        };
    }

    public async Task RegisterDeviceAsync(DeviceRegistration registration)
    {
        var now = DateTime.UtcNow;
        using var connection = dbFactory.GetCommonConnection();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Device (DeviceId, Name, Platform, "Group", Status, IsEnabled, RegisteredAt, LastConnectedAt, CreatedAt, UpdatedAt)
            VALUES (@DeviceId, @Name, @Platform, @Group, @Status, 1, @RegisteredAt, @LastConnectedAt, @CreatedAt, @UpdatedAt)
            ON CONFLICT(DeviceId) DO UPDATE SET
                Name = @Name, Platform = @Platform, "Group" = @Group,
                LastConnectedAt = @LastConnectedAt, UpdatedAt = @UpdatedAt
            """;
        command.Parameters.AddWithValue("@DeviceId", registration.DeviceId);
        command.Parameters.AddWithValue("@Name", registration.Name);
        command.Parameters.AddWithValue("@Platform", (object?)registration.Platform ?? DBNull.Value);
        command.Parameters.AddWithValue("@Group", (object?)registration.Group ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", (int)DeviceConnectionStatus.Active);
        command.Parameters.AddWithValue("@RegisteredAt", FormatDateTime(now));
        command.Parameters.AddWithValue("@LastConnectedAt", FormatDateTime(now));
        command.Parameters.AddWithValue("@CreatedAt", FormatDateTime(now));
        command.Parameters.AddWithValue("@UpdatedAt", FormatDateTime(now));
        await command.ExecuteNonQueryAsync();

        using var statusCmd = connection.CreateCommand();
        statusCmd.CommandText = """
            INSERT INTO DeviceStatus (DeviceId, Timestamp) VALUES (@DeviceId, @Timestamp)
            ON CONFLICT(DeviceId) DO NOTHING
            """;
        statusCmd.Parameters.AddWithValue("@DeviceId", registration.DeviceId);
        statusCmd.Parameters.AddWithValue("@Timestamp", FormatDateTime(now));
        await statusCmd.ExecuteNonQueryAsync();

        dbFactory.InitializeDeviceDatabase(registration.DeviceId);
        logger.LogInformation("Device registered: {DeviceId} ({Name})", registration.DeviceId, registration.Name);
    }

    public async Task UpdateDeviceAsync(string deviceId, DeviceUpdateRequest request)
    {
        using var connection = dbFactory.GetCommonConnection();
        var setClauses = new List<string>();
        using var command = connection.CreateCommand();

        if (request.Name is not null)
        {
            setClauses.Add("Name = @Name");
            command.Parameters.AddWithValue("@Name", request.Name);
        }

        if (request.Group is not null)
        {
            setClauses.Add("\"Group\" = @Group");
            command.Parameters.AddWithValue("@Group", request.Group);
        }

        if (request.Tags is not null)
        {
            setClauses.Add("Tags = @Tags");
            command.Parameters.AddWithValue("@Tags", JsonSerializer.Serialize(request.Tags));
        }

        if (request.Note is not null)
        {
            setClauses.Add("Note = @Note");
            command.Parameters.AddWithValue("@Note", request.Note);
        }

        if (request.IsEnabled.HasValue)
        {
            setClauses.Add("IsEnabled = @IsEnabled");
            command.Parameters.AddWithValue("@IsEnabled", request.IsEnabled.Value ? 1 : 0);
        }

        if (setClauses.Count == 0)
        {
            return;
        }

        setClauses.Add("UpdatedAt = @UpdatedAt");
        command.Parameters.AddWithValue("@UpdatedAt", FormatDateTime(DateTime.UtcNow));
        command.Parameters.AddWithValue("@DeviceId", deviceId);
        command.CommandText = $"UPDATE Device SET {string.Join(", ", setClauses)} WHERE DeviceId = @DeviceId";
        await command.ExecuteNonQueryAsync();
        logger.LogInformation("Device updated: {DeviceId}", deviceId);
    }

    public async Task DeleteDeviceAsync(string deviceId)
    {
        using var connection = dbFactory.GetCommonConnection();

        using var statusCmd = connection.CreateCommand();
        statusCmd.CommandText = "DELETE FROM DeviceStatus WHERE DeviceId = @DeviceId";
        statusCmd.Parameters.AddWithValue("@DeviceId", deviceId);
        await statusCmd.ExecuteNonQueryAsync();

        using var deviceCmd = connection.CreateCommand();
        deviceCmd.CommandText = "DELETE FROM Device WHERE DeviceId = @DeviceId";
        deviceCmd.Parameters.AddWithValue("@DeviceId", deviceId);
        await deviceCmd.ExecuteNonQueryAsync();

        dbFactory.DeleteDeviceDatabase(deviceId);
        logger.LogInformation("Device deleted: {DeviceId}", deviceId);
    }

    public async Task UpdateStatusAsync(string deviceId, DeviceStatusReport report)
    {
        using var connection = dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO DeviceStatus (DeviceId, Level, Progress, Battery, WifiRssi, Latitude, Longitude, CustomData, Timestamp)
            VALUES (@DeviceId, @Level, @Progress, @Battery, @WifiRssi, @Latitude, @Longitude, @CustomData, @Timestamp)
            ON CONFLICT(DeviceId) DO UPDATE SET
                Level = @Level, Progress = @Progress, Battery = @Battery, WifiRssi = @WifiRssi,
                Latitude = @Latitude, Longitude = @Longitude, CustomData = @CustomData, Timestamp = @Timestamp
            """;
        command.Parameters.AddWithValue("@DeviceId", deviceId);
        command.Parameters.AddWithValue("@Level", report.Level);
        command.Parameters.AddWithValue("@Progress", report.Progress);
        command.Parameters.AddWithValue("@Battery", (object?)report.Battery ?? DBNull.Value);
        command.Parameters.AddWithValue("@WifiRssi", (object?)report.WifiRssi ?? DBNull.Value);
        command.Parameters.AddWithValue("@Latitude", (object?)report.Latitude ?? DBNull.Value);
        command.Parameters.AddWithValue("@Longitude", (object?)report.Longitude ?? DBNull.Value);
        command.Parameters.AddWithValue("@CustomData", report.CustomData is not null ? JsonSerializer.Serialize(report.CustomData) : DBNull.Value);
        command.Parameters.AddWithValue("@Timestamp", FormatDateTime(report.Timestamp));
        await command.ExecuteNonQueryAsync();

        using var deviceConn = dbFactory.GetDeviceConnection(deviceId);
        using var historyCmd = deviceConn.CreateCommand();
        historyCmd.CommandText = """
            INSERT INTO StatusHistory (Level, Progress, Battery, Latitude, Longitude, CustomData, Timestamp)
            VALUES (@Level, @Progress, @Battery, @Latitude, @Longitude, @CustomData, @Timestamp)
            """;
        historyCmd.Parameters.AddWithValue("@Level", report.Level);
        historyCmd.Parameters.AddWithValue("@Progress", report.Progress);
        historyCmd.Parameters.AddWithValue("@Battery", (object?)report.Battery ?? DBNull.Value);
        historyCmd.Parameters.AddWithValue("@Latitude", (object?)report.Latitude ?? DBNull.Value);
        historyCmd.Parameters.AddWithValue("@Longitude", (object?)report.Longitude ?? DBNull.Value);
        historyCmd.Parameters.AddWithValue("@CustomData", report.CustomData is not null ? JsonSerializer.Serialize(report.CustomData) : DBNull.Value);
        historyCmd.Parameters.AddWithValue("@Timestamp", FormatDateTime(report.Timestamp));
        await historyCmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateConnectionStatusAsync(string deviceId, DeviceConnectionStatus status)
    {
        var now = DateTime.UtcNow;
        using var connection = dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Device SET Status = @Status, LastConnectedAt = @LastConnectedAt, UpdatedAt = @UpdatedAt
            WHERE DeviceId = @DeviceId
            """;
        command.Parameters.AddWithValue("@DeviceId", deviceId);
        command.Parameters.AddWithValue("@Status", (int)status);
        command.Parameters.AddWithValue("@LastConnectedAt", FormatDateTime(now));
        command.Parameters.AddWithValue("@UpdatedAt", FormatDateTime(now));
        await command.ExecuteNonQueryAsync();
    }

    public async Task<StatusSummary> GetStatusSummaryAsync()
    {
        using var connection = dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Status, COUNT(*) FROM Device GROUP BY Status";

        int active = 0, inactive = 0, warning = 0, error = 0;
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var status = (DeviceConnectionStatus)reader.GetInt32(0);
            var count = reader.GetInt32(1);
            switch (status)
            {
                case DeviceConnectionStatus.Active: active = count; break;
                case DeviceConnectionStatus.Inactive: inactive = count; break;
                case DeviceConnectionStatus.Warning: warning = count; break;
                case DeviceConnectionStatus.Error: error = count; break;
            }
        }

        return new StatusSummary { Active = active, Inactive = inactive, Warning = warning, Error = error };
    }

    public async Task<List<DeviceStatusReport>> GetStatusHistoryAsync(string deviceId, DateTime? from = null, DateTime? to = null)
    {
        using var connection = dbFactory.GetDeviceConnection(deviceId);
        using var command = connection.CreateCommand();
        var sql = "SELECT Level, Progress, Battery, Latitude, Longitude, CustomData, Timestamp FROM StatusHistory";
        var conditions = new List<string>();

        if (from.HasValue)
        {
            conditions.Add("Timestamp >= @From");
            command.Parameters.AddWithValue("@From", FormatDateTime(from.Value));
        }

        if (to.HasValue)
        {
            conditions.Add("Timestamp <= @To");
            command.Parameters.AddWithValue("@To", FormatDateTime(to.Value));
        }

        if (conditions.Count > 0)
        {
            sql += " WHERE " + string.Join(" AND ", conditions);
        }

        sql += " ORDER BY Timestamp DESC";
        command.CommandText = sql;

        var history = new List<DeviceStatusReport>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            history.Add(new DeviceStatusReport
            {
                Level = reader.GetInt32(0),
                Progress = reader.GetDouble(1),
                Battery = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Latitude = reader.IsDBNull(3) ? null : reader.GetDouble(3),
                Longitude = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                CustomData = reader.IsDBNull(5) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(5)),
                Timestamp = ParseDateTime(reader.GetString(6))
            });
        }

        return history;
    }

    private static string FormatDateTime(DateTime dt) => dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    private static DateTime ParseDateTime(string s) => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static string[]? ParseTags(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.TrimStart().StartsWith('['))
        {
            return JsonSerializer.Deserialize<string[]>(value);
        }

        return value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }
}
