namespace WorkDesignBlazor.Data;

public class LogEntry
{
    public DateTime DateTime { get; set; }

    public string Customer { get; set; } = default!;

    public string Device { get; set; } = default!;

    public int Type { get; set; }

    public int Level { get; set; }

    public int Parts { get; set; }

    public double Value { get; set; }

    public string Description { get; set; } = default!;
}
