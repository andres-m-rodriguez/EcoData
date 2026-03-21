using MudBlazor;

namespace EcoPortal.Client.Themes;

public static class AppThemes
{
    // Material Design 3 color palette based on EcoData brand
    public static readonly MudTheme Azure = new()
    {
        PaletteLight = new PaletteLight
        {
            // Primary - Deep ocean blue
            Primary = "#003452",
            PrimaryContrastText = "#ffffff",
            // Secondary - Vibrant blue
            Secondary = "#005c91",
            SecondaryContrastText = "#ffffff",
            // Tertiary - Warm earth
            Tertiary = "#3e1d00",
            TertiaryContrastText = "#ffffff",
            // Semantic colors
            Info = "#005c91",
            InfoLighten = "#cde5ff",
            Success = "#16A34A",
            SuccessLighten = "#F0FDF4",
            Warning = "#D97706",
            WarningLighten = "#FFFBEB",
            Error = "#ba1a1a",
            ErrorLighten = "#ffdad6",
            // App bar
            AppbarBackground = "#003452",
            AppbarText = "#ffffff",
            // Drawer
            DrawerBackground = "#f9f9fb",
            DrawerText = "#191c1c",
            // Surfaces
            Background = "#f9f9fb",
            BackgroundGray = "#f0f1f3",
            Surface = "#ffffff",
            // Text
            TextPrimary = "#191c1c",
            TextSecondary = "#414844",
            TextDisabled = "#717973",
            // Lines and dividers
            Divider = "#c1c8c2",
            LinesDefault = "#c1c8c2",
            LinesInputs = "#717973",
            // Actions
            ActionDefault = "#717973",
            ActionDisabled = "#c1c8c2",
            HoverOpacity = 0.04,
        },
        PaletteDark = new PaletteDark
        {
            // Primary - Light blue for dark mode
            Primary = "#94ccff",
            PrimaryContrastText = "#003452",
            // Secondary
            Secondary = "#94ccff",
            SecondaryContrastText = "#001d32",
            // Tertiary
            Tertiary = "#f7ba8b",
            TertiaryContrastText = "#2f1500",
            // Semantic colors
            Info = "#94ccff",
            InfoLighten = "#004972",
            Success = "#4ADE80",
            SuccessLighten = "#14532D",
            Warning = "#FBBF24",
            WarningLighten = "#451A03",
            Error = "#ffb4ab",
            ErrorLighten = "#93000a",
            // App bar
            AppbarBackground = "#1c1c1e",
            AppbarText = "#f3f2f1",
            // Drawer
            DrawerBackground = "#1c1c1e",
            DrawerText = "#f3f2f1",
            // Surfaces
            Background = "#121212",
            BackgroundGray = "#1c1c1e",
            Surface = "#1c1c1e",
            // Text
            TextPrimary = "#e1e3e2",
            TextSecondary = "#c1c8c2",
            TextDisabled = "#717973",
            // Lines and dividers
            Divider = "#414844",
            LinesDefault = "#414844",
            LinesInputs = "#717973",
            // Actions
            ActionDefault = "#c1c8c2",
            ActionDisabled = "#414844",
            HoverOpacity = 0.08,
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "sans-serif"],
            },
            H1 = new H1Typography
            {
                FontFamily = ["Newsreader", "serif"],
                FontWeight = "700",
            },
            H2 = new H2Typography
            {
                FontFamily = ["Newsreader", "serif"],
                FontWeight = "700",
            },
            H3 = new H3Typography
            {
                FontFamily = ["Newsreader", "serif"],
                FontWeight = "600",
            },
            H4 = new H4Typography
            {
                FontFamily = ["Newsreader", "serif"],
                FontWeight = "600",
            },
            H5 = new H5Typography
            {
                FontFamily = ["Newsreader", "serif"],
                FontWeight = "600",
            },
            H6 = new H6Typography
            {
                FontFamily = ["Newsreader", "serif"],
                FontWeight = "600",
            },
        },
    };
}
