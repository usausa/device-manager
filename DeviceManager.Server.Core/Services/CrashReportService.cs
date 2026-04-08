namespace DeviceManager.Server.Core.Services;

using System.Globalization;
using System.Text.Json;

using DeviceManager.Server.Core.Database;
using DeviceManager.Shared.Models;

using Microsoft.Extensions.Logging;

/// <summary>
/// Stores and retrieves crash reports sent from devices.
/// </summary>
public sealed class CrashReportService(DbConnectionFactory dbFactory, ILogger<CrashReportService> logger)
{
    /// <summary>Persists a crash report and returns the saved record with its assigned <see cref="CrashReport.ReportId"/>.</summary>
    public async Task<CrashReport> AddReportAsync(CrashReport report)
    {
        using var connection = dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO CrashReport
                (DeviceId, ExceptionType, Message, StackTrace, InnerException,
                 AppVersion, OsVersion, AdditionalData, OccurredAt, ReceivedAt)
            VALUES
                (@DeviceId, @ExceptionType, @Message, @StackTrace, @InnerException,
                 @AppVersion, @OsVersion, @AdditionalData, @OccurredAt, @ReceivedAt);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("@DeviceId", report.DeviceId);
        command.Parameters.AddWithValue("@ExceptionType", report.ExceptionType);
        command.Parameters.AddWithValue("@Message", report.Message);
        command.Parameters.AddWithValue("@StackTrace", (object?)report.StackTrace ?? DBNull.Value);
        command.Parameters.AddWithValue("@InnerException", (object?)report.InnerException ?? DBNull.Value);
        command.Parameters.AddWithValue("@AppVersion", (object?)report.AppVersion ?? DBNull.Value);
        command.Parameters.AddWithValue("@OsVersion", (object?)report.OsVersion ?? DBNull.Value);
        command.Parameters.AddWithValue("@AdditionalData",
            report.AdditionalData is not null ? JsonSerializer.Serialize(report.AdditionalData) : DBNull.Value);
        command.Parameters.AddWithValue("@OccurredAt", FormatDateTime(report.OccurredAt));
        command.Parameters.AddWithValue("@ReceivedAt", FormatDateTime(report.ReceivedAt));

        var reportId = Convert.ToInt64(await command.ExecuteScalarAsync());
        logger.LogInformation("Crash report saved: #{ReportId} from {DeviceId} ({ExceptionType})",
            reportId, report.DeviceId, report.ExceptionType);

        return new CrashReport
        {
            ReportId = reportId,
            DeviceId = report.DeviceId,
            ExceptionType = report.ExceptionType,
            Message = report.Message,
            StackTrace = report.StackTrace,
            InnerException = report.InnerException,
            AppVersion = report.AppVersion,
            OsVersion = report.OsVersion,
            AdditionalData = report.AdditionalData,
            OccurredAt = report.OccurredAt,
            ReceivedAt = report.ReceivedAt
        };
    }

    /// <summary>Returns crash reports in descending order of occurrence, optionally filtered by device.</summary>
    public async Task<List<CrashReport>> GetReportsAsync(string? deviceId, int skip, int take)
    {
        using var connection = dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();

        var sql = """
            SELECT ReportId, DeviceId, ExceptionType, Message, StackTrace, InnerException,
                   AppVersion, OsVersion, AdditionalData, OccurredAt, ReceivedAt
            FROM CrashReport
            """;

        if (deviceId is not null)
        {
            sql += " WHERE DeviceId = @DeviceId";
            command.Parameters.AddWithValue("@DeviceId", deviceId);
        }

        sql += " ORDER BY OccurredAt DESC LIMIT @Take OFFSET @Skip";
        command.Parameters.AddWithValue("@Take", take);
        command.Parameters.AddWithValue("@Skip", skip);
        command.CommandText = sql;

        var reports = new List<CrashReport>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            reports.Add(ReadReport(reader));
        }

        return reports;
    }

    /// <summary>Returns a single crash report by ID, or <see langword="null"/> if not found.</summary>
    public async Task<CrashReport?> GetReportAsync(long reportId)
    {
        using var connection = dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ReportId, DeviceId, ExceptionType, Message, StackTrace, InnerException,
                   AppVersion, OsVersion, AdditionalData, OccurredAt, ReceivedAt
            FROM CrashReport
            WHERE ReportId = @ReportId
            """;
        command.Parameters.AddWithValue("@ReportId", reportId);

        using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadReport(reader) : null;
    }

    /// <summary>Deletes a crash report by ID.</summary>
    public async Task DeleteReportAsync(long reportId)
    {
        using var connection = dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM CrashReport WHERE ReportId = @ReportId";
        command.Parameters.AddWithValue("@ReportId", reportId);
        await command.ExecuteNonQueryAsync();
        logger.LogInformation("Crash report deleted: #{ReportId}", reportId);
    }

    private static CrashReport ReadReport(System.Data.IDataReader reader)
    {
        var additionalDataJson = reader.IsDBNull(8) ? null : reader.GetString(8);
        return new CrashReport
        {
            ReportId = reader.GetInt64(0),
            DeviceId = reader.GetString(1),
            ExceptionType = reader.GetString(2),
            Message = reader.GetString(3),
            StackTrace = reader.IsDBNull(4) ? null : reader.GetString(4),
            InnerException = reader.IsDBNull(5) ? null : reader.GetString(5),
            AppVersion = reader.IsDBNull(6) ? null : reader.GetString(6),
            OsVersion = reader.IsDBNull(7) ? null : reader.GetString(7),
            AdditionalData = additionalDataJson is not null
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(additionalDataJson)
                : null,
            OccurredAt = ParseDateTime(reader.GetString(9)),
            ReceivedAt = ParseDateTime(reader.GetString(10))
        };
    }

    private static string FormatDateTime(DateTime dt) =>
        dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    private static DateTime ParseDateTime(string s) =>
        DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
