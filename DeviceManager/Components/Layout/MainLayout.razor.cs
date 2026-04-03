namespace DeviceManager.Components.Layout;

public partial class MainLayout
{
    private bool drawerOpen = true;

    private readonly MudTheme theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Colors.Blue.Default,
            Secondary = "#00C853",
            AppbarBackground = Colors.Blue.Darken4,
            AppbarText = Colors.Shades.White,
            DrawerBackground = Colors.Shades.White,
            Background = "#FAFAFA",
            Surface = Colors.Shades.White,
        }
    };

    private void ToggleDrawer() => drawerOpen = !drawerOpen;
}
