namespace WorkDesignBlazor.Data;

public class StatusEntity
{
    public bool Enabled { get; set; }

    public string Name { get; set; } = default!;

    public int Wifi { get; set; }

    public int Battery { get; set; }

    public int Progress1 { get; set; }

    public int Progress2 { get; set; }

    public DateTime DateTime { get; set; }
}
