namespace DeviceMonitor.Server.Services;

using DeviceMonitor.Server.Models;

using Smart.Data;
using Smart.Data.Mapper;

public class DataService
{
    private readonly IDbProvider dbProvider;

    public DataService(IDbProvider dbProvider)
    {
        this.dbProvider = dbProvider;
    }

    public ValueTask<int> UpdateStatusAsync(StatusEntity entity) =>
        dbProvider.UsingAsync(con =>
            con.ExecuteAsync(
                "UPDATE Status SET " +
                "Timestamp = @Timestamp, " +
                "Battery = @Battery, " +
                "Longitude = COALESCE(@Longitude, Longitude), " +
                "Latitude = COALESCE(@Latitude, Latitude), " +
                "LastLocationAt = COALESCE(@LastLocationAt, LastLocationAt) " +
                "WHERE Id = @Id",
                entity));

    //public ValueTask<List<StatusEntity>> QueryStatusListAsync() =>
    //    dbProvider.UsingAsync(con =>
    //        con.QueryListAsync<StatusEntity>("SELECT * FROM Status ORDER BY Id"));

    // TODO
    public ValueTask<List<StatusEntity>> QueryStatusListAsync() => ValueTask.FromResult(new List<StatusEntity>());
}
