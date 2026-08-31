namespace DeviceManager.Services;

using DeviceManager.Accessors;

public sealed class DeviceService
{
    private readonly DeviceAccessor deviceAccessor;

    private readonly StatusAccessor statusAccessor;

    private readonly ConnectionLogAccessor connectionLogAccessor;

    private readonly IDialect dialect;

    private readonly TimeProvider timeProvider;

    public DeviceService(
        DeviceAccessor deviceAccessor,
        StatusAccessor statusAccessor,
        ConnectionLogAccessor connectionLogAccessor,
        IDialect dialect,
        TimeProvider timeProvider)
    {
        this.deviceAccessor = deviceAccessor;
        this.statusAccessor = statusAccessor;
        this.connectionLogAccessor = connectionLogAccessor;
        this.dialect = dialect;
        this.timeProvider = timeProvider;
    }

    public void CreateTable()
    {
        deviceAccessor.Create();
        statusAccessor.Create();
        connectionLogAccessor.Create();
    }

    //--------------------------------------------------------------------------------
    // Query
    //--------------------------------------------------------------------------------

    public ValueTask<List<DeviceListView>> QueryListAsync(string? filter) =>
        deviceAccessor.QueryListAsync(String.IsNullOrEmpty(filter) ? null : filter);

    public ValueTask<DeviceEntity?> QueryAsync(string deviceId) =>
        deviceAccessor.QueryAsync(deviceId);

    public ValueTask<DeviceStatusEntity?> QueryStatusAsync(string deviceId) =>
        statusAccessor.QueryAsync(deviceId);

    //--------------------------------------------------------------------------------
    // Registration
    //--------------------------------------------------------------------------------

    // 端末登録(未登録なら追加、登録済みなら情報更新)し、稼働状態へ遷移する
    public async ValueTask RegisterAsync(DeviceRegistration registration)
    {
        var now = timeProvider.GetLocalNow().DateTime;

        var device = await deviceAccessor.QueryAsync(registration.DeviceId);
        if (device is null)
        {
            await deviceAccessor.InsertAsync(registration.DeviceId, registration.Name, registration.Platform, (int)DeviceState.Active, now, now);
        }
        else
        {
            await deviceAccessor.UpdateRegisterAsync(registration.DeviceId, registration.Name, registration.Platform, (int)DeviceState.Active, now);
        }

        await connectionLogAccessor.InsertAsync(registration.DeviceId, "Connected", now);
    }

    // 切断時に停止状態へ遷移する
    public async ValueTask DisconnectAsync(string deviceId)
    {
        var now = timeProvider.GetLocalNow().DateTime;

        await deviceAccessor.UpdateStateAsync(deviceId, (int)DeviceState.Inactive);
        await connectionLogAccessor.InsertAsync(deviceId, "Disconnected", now);
    }

    //--------------------------------------------------------------------------------
    // Status
    //--------------------------------------------------------------------------------

    // ステータス報告(現在値の更新 + 履歴の追加 + 端末状態の更新)。measuredAt は計測時刻(再送考慮)
    public async ValueTask ReportStatusAsync(string deviceId, DeviceStatusReport report, DateTime? measuredAt = null)
    {
        var now = timeProvider.GetLocalNow().DateTime;
        var timestamp = measuredAt ?? now;
        var moving = report.Moving ? 1 : 0;

        await statusAccessor.UpsertAsync(deviceId, report.Level, report.Battery, report.WifiRssi, report.ApName, moving, report.ScanCount, report.Progress1, report.Progress2, report.Latitude, report.Longitude, timestamp);
        await statusAccessor.InsertHistoryAsync(deviceId, report.Level, report.Battery, report.WifiRssi, report.ApName, moving, report.ScanCount, report.Progress1, report.Progress2, report.Latitude, report.Longitude, timestamp);

        var state = report.Level switch
        {
            2 => DeviceState.Error,
            1 => DeviceState.Warning,
            _ => DeviceState.Active
        };
        await deviceAccessor.UpdateConnectedAsync(deviceId, (int)state, now);
    }

    //--------------------------------------------------------------------------------
    // Maintenance
    //--------------------------------------------------------------------------------

    // 未登録端末をテレメトリ受信時に自動登録する(名称 = 端末ID)
    public async ValueTask EnsureAsync(string deviceId)
    {
        var device = await deviceAccessor.QueryAsync(deviceId);
        if (device is null)
        {
            await AddAsync(deviceId, deviceId, null, null);
        }
    }

    // 管理画面からの手動追加(重複は false)
    public async ValueTask<bool> AddAsync(string deviceId, string name, string? groupName, string? note)
    {
        var now = timeProvider.GetLocalNow().DateTime;
        try
        {
            await deviceAccessor.InsertAsync(deviceId, name, null, (int)DeviceState.Inactive, now, null);
        }
        catch (DbException ex)
        {
            if (dialect.IsDuplicate(ex))
            {
                return false;
            }

            throw;
        }

        await deviceAccessor.UpdateProfileAsync(deviceId, name, groupName, note, 1);
        return true;
    }

    public async ValueTask<bool> UpdateProfileAsync(string deviceId, string name, string? groupName, string? note, bool isEnabled)
    {
        var rows = await deviceAccessor.UpdateProfileAsync(deviceId, name, groupName, note, isEnabled ? 1 : 0);
        return rows > 0;
    }

    // 端末本体と付随情報(現在値・履歴・接続ログ)を削除する
    public async ValueTask<bool> DeleteAsync(string deviceId)
    {
        var rows = await deviceAccessor.DeleteAsync(deviceId);
        await statusAccessor.DeleteAsync(deviceId);
        await statusAccessor.DeleteHistoryByDeviceAsync(deviceId);
        await connectionLogAccessor.DeleteByDeviceAsync(deviceId);
        return rows > 0;
    }

    //--------------------------------------------------------------------------------
    // Summary
    //--------------------------------------------------------------------------------

    public ValueTask<int> CountAsync() =>
        deviceAccessor.CountAsync();

    public ValueTask<int> CountByStateAsync(DeviceState state) =>
        deviceAccessor.CountByStateAsync((int)state);

    public ValueTask<int> CountActiveSinceAsync(DateTime since) =>
        statusAccessor.CountActiveSinceAsync(since);

    public ValueTask<int> CountLowBatterySinceAsync(double threshold, DateTime since) =>
        statusAccessor.CountLowBatterySinceAsync(threshold, since);

    //--------------------------------------------------------------------------------
    // Cleanup
    //--------------------------------------------------------------------------------

    public async ValueTask CleanupAsync(DateTime threshold)
    {
        await statusAccessor.DeleteHistoryOlderAsync(threshold);
        await connectionLogAccessor.DeleteOlderAsync(threshold);
    }
}
