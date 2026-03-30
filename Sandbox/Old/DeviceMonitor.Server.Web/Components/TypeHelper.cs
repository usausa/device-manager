namespace DeviceMonitor.Server.Web.Components;

using System.Runtime.CompilerServices;

public static class TypeHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<T, bool> FilterBy<T>(Func<T, bool> func) => func;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Func<T, object?> SortBy<T>(Func<T, object?> func) => func;
}
