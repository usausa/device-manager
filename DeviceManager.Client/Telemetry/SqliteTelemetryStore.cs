namespace DeviceManager.Client.Telemetry;

using Microsoft.Data.Sqlite;

// SQLite によるタンキングプロバイダー(プロセス再起動後も未送信分を再送できる)
public sealed class SqliteTelemetryStore : ITelemetryStore
{
    private readonly string connectionString;

    public SqliteTelemetryStore(SqliteTelemetryStoreOptions options)
    {
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = options.FilePath,
            Pooling = true
        }.ToString();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "CREATE TABLE IF NOT EXISTS Telemetry (" +
            "Id INTEGER NOT NULL, " +
            "Kind INTEGER NOT NULL, " +
            "Payload TEXT NOT NULL, " +
            "CreatedAt INTEGER NOT NULL, " +
            "PRIMARY KEY (Id AUTOINCREMENT))";
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        // プールを解放してファイルハンドルを閉じる
        using var connection = new SqliteConnection(connectionString);
        SqliteConnection.ClearPool(connection);
    }

    public async ValueTask AddAsync(TelemetryEnvelope envelope, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Telemetry (Kind, Payload, CreatedAt) VALUES (@kind, @payload, @createdAt)";
        command.Parameters.AddWithValue("@kind", (int)envelope.Kind);
        command.Parameters.AddWithValue("@payload", envelope.Payload);
        command.Parameters.AddWithValue("@createdAt", envelope.CreatedAt.Ticks);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<TelemetryEnvelope>> PeekAsync(int max, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Kind, Payload, CreatedAt FROM Telemetry ORDER BY Id LIMIT @max";
        command.Parameters.AddWithValue("@max", max);

        var list = new List<TelemetryEnvelope>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(new TelemetryEnvelope
            {
                Id = reader.GetInt64(0),
                Kind = (TelemetryKind)reader.GetInt32(1),
                Payload = reader.GetString(2),
                CreatedAt = new DateTime(reader.GetInt64(3))
            });
        }

        return list;
    }

    public async ValueTask RemoveAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return;
        }

        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM Telemetry WHERE Id = @id";
        var parameter = command.Parameters.Add("@id", SqliteType.Integer);
        foreach (var id in ids)
        {
            parameter.Value = id;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask CleanupAsync(DateTime threshold, int maxItems, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "DELETE FROM Telemetry WHERE CreatedAt < @threshold";
            command.Parameters.AddWithValue("@threshold", threshold.Ticks);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "DELETE FROM Telemetry WHERE Id NOT IN (SELECT Id FROM Telemetry ORDER BY Id DESC LIMIT @max)";
            command.Parameters.AddWithValue("@max", maxItems);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask<int> CountAsync(CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Telemetry";
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }
}
