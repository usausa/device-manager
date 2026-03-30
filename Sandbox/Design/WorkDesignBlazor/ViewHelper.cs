namespace WorkDesignBlazor;

using MudBlazor;

public static class ViewHelper
{
    public static string ValueColorStyle(double value)
    {
        if (value < 0)
        {
            return "mud-error-text";
        }
        if (value > 0)
        {
            return "mud-success-text";
        }

        return "";
    }

    public static string StatusColor(bool status)
    {
        return status ? Colors.Green.Accent4 : Colors.Grey.Default;
    }

    public static string WifiIcon(double level)
    {
        if (level > -67)
        {
            return Icons.Filled.SignalWifi4Bar;
        }
        if (level > -70)
        {
            return Icons.Filled.NetworkWifi3Bar;
        }
        if (level > -80)
        {
            return Icons.Filled.NetworkWifi2Bar;
        }
        if (level > -90)
        {
            return Icons.Filled.NetworkWifi1Bar;
        }
        return Icons.Filled.SignalWifi0Bar;
    }

    public static string WifiColor(double level)
    {
        if (level > -70)
        {
            return Colors.Green.Accent4;
        }
        if (level > -90)
        {
            return Colors.Amber.Accent4;
        }
        return Colors.Grey.Default;
    }

    public static string BatteryIcon(double level)
    {
        if (level > 87.5)
        {
            return Icons.Filled.BatteryFull;
        }
        if (level > 75)
        {
            return Icons.Filled.Battery6Bar;
        }
        if (level > 62.5)
        {
            return Icons.Filled.Battery5Bar;
        }
        if (level > 50)
        {
            return Icons.Filled.Battery4Bar;
        }
        if (level > 37.5)
        {
            return Icons.Filled.Battery3Bar;
        }
        if (level > 25)
        {
            return Icons.Filled.Battery2Bar;
        }
        if (level > 12.5)
        {
            return Icons.Filled.Battery1Bar;
        }
        return Icons.Filled.Battery0Bar;
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
