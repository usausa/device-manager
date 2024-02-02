namespace DeviceManager.Server.Web.Shared;

public sealed partial class MainLayout
{
    private ErrorBoundary? errorBoundary;

    private bool drawerOpen = true;

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    protected override void OnParametersSet()
    {
        errorBoundary?.Recover();
    }

    private void DrawerToggle()
    {
        drawerOpen = !drawerOpen;
    }
}
