using MudBlazor;

namespace EcoPortal.Client.Themes;

public static class AppThemes
{
    public static readonly MudTheme Azure = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0078d4",
            Secondary = "#2b88d8",
            Tertiary = "#106ebe",
            Info = "#2563EB",
            InfoLighten = "#EFF6FF",
            Success = "#16A34A",
            SuccessLighten = "#F0FDF4",
            Warning = "#D97706",
            WarningLighten = "#FFFBEB",
            AppbarBackground = "#0078d4",
            AppbarText = "#ffffff",
            DrawerBackground = "#F9F9FB",
            DrawerText = "#262626",
            Background = "#ffffff",
            Surface = "#ffffff",
            TextPrimary = "#262626",
            TextSecondary = "#4D4D4D",
            ActionDefault = "#6b7280",
            HoverOpacity = 0.04,
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#2899f5",
            Secondary = "#3aa0f3",
            Tertiary = "#6cb8f6",
            Info = "#60A5FA",
            InfoLighten = "#1E3A5F",
            Success = "#4ADE80",
            SuccessLighten = "#14532D",
            Warning = "#FBBF24",
            WarningLighten = "#451A03",
            AppbarBackground = "#1b1a19",
            AppbarText = "#ffffff",
            DrawerBackground = "#252423",
            DrawerText = "#f3f2f1",
            Background = "#1b1a19",
            Surface = "#252423",
            TextPrimary = "#f3f2f1",
            TextSecondary = "#a19f9d",
            ActionDefault = "#9CA3AF",
            HoverOpacity = 0.08,
        },
    };
}
