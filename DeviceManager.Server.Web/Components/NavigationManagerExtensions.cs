namespace DeviceManager.Server.Web.Components;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

public static class NavigationManagerExtensions
{
    public static Dictionary<string, StringValues> ExtractQuery(this NavigationManager manager)
    {
        var uri = manager.ToAbsoluteUri(manager.Uri);
        return QueryHelpers.ParseQuery(uri.Query);
    }

    public static void UpdateParameter(this NavigationManager navigationManager, string name, bool value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, bool? value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, DateTime value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, DateTime? value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, DateOnly value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, DateOnly? value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, TimeOnly value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, TimeOnly? value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, decimal value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, decimal? value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, double value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, double? value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, float value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, float? value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, Guid value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, Guid? value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, int value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, int? value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, long value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, long? value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameter(this NavigationManager navigationManager, string name, string? value) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameter(name, value), false, true);

    public static void UpdateParameters(this NavigationManager navigationManager, IReadOnlyDictionary<string, object?> parameters) =>
        navigationManager.NavigateTo(navigationManager.GetUriWithQueryParameters(parameters), false, true);
}
