namespace DeviceManager.Server.Web.Shared;

public sealed partial class SimpleLayout
{
    private ErrorBoundary? errorBoundary;

    protected override void OnParametersSet()
    {
        errorBoundary?.Recover();
    }
}
