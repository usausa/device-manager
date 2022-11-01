namespace DeviceManager.Server.Web.Shared;

public sealed partial class MainLayout
{
    private ErrorBoundary? errorBoundary;

    private bool drawerOpen = true;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override void OnParametersSet()
    {
        errorBoundary?.Recover();
    }

    private void DrawerToggle()
    {
        drawerOpen = !drawerOpen;
    }
}
