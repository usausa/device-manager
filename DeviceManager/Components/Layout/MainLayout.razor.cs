namespace DeviceManager.Components.Layout;

using Microsoft.AspNetCore.Components;

public partial class MainLayout
{
    private bool sidebarOpen;

    private void ToggleSidebar()
    {
        sidebarOpen = !sidebarOpen;
    }
}
