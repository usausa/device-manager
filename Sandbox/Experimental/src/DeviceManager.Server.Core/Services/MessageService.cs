using System.Globalization;
using DeviceManager.Server.Core.Database;
using DeviceManager.Shared.Models;
using Microsoft.Extensions.Logging;

namespace DeviceManager.Server.Core.Services;

public sealed class MessageService
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly ILogger<MessageService> _logger;

    public MessageService(DbConnectionFactory dbFactory, ILogger<MessageService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task AddMessageAsync(ServerMessage message)
    {
        using var connection = _dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Message (DeviceId, Direction, MessageType, Content, Status, CreatedAt)
            VALUES (@DeviceId, @Direction, @MessageType, @Content, @Status, @CreatedAt)
            """;
        command.Parameters.AddWithValue("@DeviceId", (object?)message.DeviceId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Direction", (int)message.Direction);
        command.Parameters.AddWithValue("@MessageType", message.MessageType);
        command.Parameters.AddWithValue("@Content", message.Content);
        command.Parameters.AddWithValue("@Status", (int)message.Status);
        command.Parameters.AddWithValue("@CreatedAt", FormatDateTime(message.CreatedAt));
        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("Message added: {MessageType} for device {DeviceId}", message.MessageType, message.DeviceId ?? "(broadcast)");
    }

    public async Task<List<ServerMessage>> GetMessagesAsync(string? deviceId, int skip, int take)
    {
        using var connection = _dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();

        var sql = "SELECT MessageId, DeviceId, Direction, MessageType, Content, Status, CreatedAt FROM Message";

        if (deviceId is not null)
        {
            sql += " WHERE DeviceId = @DeviceId";
            command.Parameters.AddWithValue("@DeviceId", deviceId);
        }

        sql += " ORDER BY CreatedAt DESC LIMIT @Take OFFSET @Skip";
        command.Parameters.AddWithValue("@Take", take);
        command.Parameters.AddWithValue("@Skip", skip);
        command.CommandText = sql;

        var messages = new List<ServerMessage>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(new ServerMessage
            {
                MessageId = reader.GetInt64(0),
                DeviceId = reader.IsDBNull(1) ? null : reader.GetString(1),
                Direction = (MessageDirection)reader.GetInt32(2),
                MessageType = reader.GetString(3),
                Content = reader.GetString(4),
                Status = (MessageStatus)reader.GetInt32(5),
                CreatedAt = ParseDateTime(reader.GetString(6))
            });
        }

        return messages;
    }

    public async Task<int> GetMessageCountAsync(string? deviceId)
    {
        using var connection = _dbFactory.GetCommonConnection();
        using var command = connection.CreateCommand();

        var sql = "SELECT COUNT(*) FROM Message";

        if (deviceId is not null)
        {
            sql += " WHERE DeviceId = @DeviceId";
            command.Parameters.AddWithValue("@DeviceId", deviceId);
        }

        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static string FormatDateTime(DateTime dt) => dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    private static DateTime ParseDateTime(string s) => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
