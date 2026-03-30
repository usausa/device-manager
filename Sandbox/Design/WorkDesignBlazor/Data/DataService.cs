namespace WorkDesignBlazor.Data;

public class DataService
{
    private readonly Random rand = new();

    public List<LogEntry> QueryLogList()
    {
        var now = DateTime.Now;

        var list = new List<LogEntry>();
        for (var i = 0; i < 50; i++)
        {
            now = now.AddSeconds(-rand.Next(10000));

            var number = (i % 10) + 1;
            list.Add(new LogEntry
            {
                DateTime = now,
                Customer = $"お客様-{number:D5}",
                Device = $"機器-{number:D5}",
                Type = number % 4,
                Level = number % 3,
                Parts = rand.Next(5),
                Value = rand.NextDouble() * 100,
                Description = "異常値が検出されました"
            });
        }
        return list;
    }

    public LogEntry CreateLog()
    {
        var now = DateTime.Now;
        var number = rand.Next(10) + 1;

        return new LogEntry
        {
            DateTime = now,
            Customer = $"お客様-{number:D5}",
            Device = $"機器-{number:D5}",
            Type = number % 4,
            Level = number % 3,
            Parts = rand.Next(5),
            Value = rand.NextDouble() * 100,
            Description = "異常値が検出されました"
        };
    }

    public List<StatusEntity> QueryStatusList()
    {
        var now = DateTime.Now;

        var list = new List<StatusEntity>();
        for (var i = 0; i < 10; i++)
        {
            list.Add(new StatusEntity
            {
                Enabled = i % 3 > 0,
                Name = $"Device-{i + 1:D5}",
                Wifi = -(30 + rand.Next(70)),
                Battery = rand.Next(100),
                Progress1 = rand.Next(1000),
                Progress2 = rand.Next(1000),
                DateTime = now.AddSeconds(-rand.Next(14400))
            });
        }
        return list;
    }
}
