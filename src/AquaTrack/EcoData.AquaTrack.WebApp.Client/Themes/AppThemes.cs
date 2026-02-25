using MudBlazor;

namespace EcoData.AquaTrack.WebApp.Client.Themes;

public static class AppThemes
{
    public static readonly MudTheme Azure = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0078d4",
            Secondary = "#2b88d8",
            Tertiary = "#106ebe",
            AppbarBackground = "#0078d4",
            AppbarText = "#ffffff",
            DrawerBackground = "#faf9f8",
            DrawerText = "#323130",
            Background = "#f3f2f1",
            Surface = "#ffffff",
            TextPrimary = "#323130",
            TextSecondary = "#605e5c"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#2899f5",
            Secondary = "#3aa0f3",
            Tertiary = "#6cb8f6",
            AppbarBackground = "#1b1a19",
            AppbarText = "#ffffff",
            DrawerBackground = "#252423",
            DrawerText = "#f3f2f1",
            Background = "#1b1a19",
            Surface = "#252423",
            TextPrimary = "#f3f2f1",
            TextSecondary = "#a19f9d"
        }
    };
}
