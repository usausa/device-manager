namespace DeviceMonitor.Server.Web.Components;

using MudBlazor;

public static class ViewHelper
{
    public static string StatusColor(bool status)
    {
        return status ? Colors.Green.Accent4 : Colors.Grey.Default;
    }

    public static string BatteryIcon(double level)
    {
        if (level > 87.5)
        {
            return Icons.Material.Filled.BatteryFull;
        }
        if (level > 75)
        {
            return Icons.Material.Filled.Battery6Bar;
        }
        if (level > 62.5)
        {
            return Icons.Material.Filled.Battery5Bar;
        }
        if (level > 50)
        {
            return Icons.Material.Filled.Battery4Bar;
        }
        if (level > 37.5)
        {
            return Icons.Material.Filled.Battery3Bar;
        }
        if (level > 25)
        {
            return Icons.Material.Filled.Battery2Bar;
        }
        if (level > 12.5)
        {
            return Icons.Material.Filled.Battery1Bar;
        }
        return Icons.Material.Filled.Battery0Bar;
    }

    public static string BatteryColor(double level)
    {
        if (level > 50)
        {
            return Colors.Green.Accent4;
        }
        if (level > 20)
        {
            return Colors.Amber.Accent4;
        }
        return Colors.Red.Accent4;
    }
}
