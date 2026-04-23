using MudBlazor;

namespace FaunaFinder.Client.Themes;

public static class FaunaFinderTheme
{
    // Nature-inspired green color palette for FaunaFinder
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            // Primary - Deep forest green
            Primary = "#2d6a4f",
            PrimaryContrastText = "#ffffff",
            PrimaryDarken = "#1b4332",
            PrimaryLighten = "#40916c",
            // Secondary - Medium green
            Secondary = "#40916c",
            SecondaryContrastText = "#ffffff",
            // Tertiary - Earth tone
            Tertiary = "#74542c",
            TertiaryContrastText = "#ffffff",
            // Semantic colors
            Info = "#0077b6",
            InfoLighten = "#caf0f8",
            Success = "#16A34A",
            SuccessLighten = "#F0FDF4",
            Warning = "#D97706",
            WarningLighten = "#FFFBEB",
            Error = "#ba1a1a",
            ErrorLighten = "#ffdad6",
            // App bar
            AppbarBackground = "#2d6a4f",
            AppbarText = "#ffffff",
            // Drawer
            DrawerBackground = "#f8f9fa",
            DrawerText = "#1b4332",
            // Surfaces
            Background = "#f8f9fa",
            BackgroundGray = "#f0f1f3",
            Surface = "#ffffff",
            // Text
            TextPrimary = "#1a1c1a",
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
            // Primary - Light green for dark mode
            Primary = "#52b788",
            PrimaryContrastText = "#1b4332",
            PrimaryDarken = "#40916c",
            PrimaryLighten = "#74c69d",
            // Secondary
            Secondary = "#74c69d",
            SecondaryContrastText = "#1b4332",
            // Tertiary
            Tertiary = "#d4a373",
            TertiaryContrastText = "#2f1500",
            // Semantic colors
            Info = "#90e0ef",
            InfoLighten = "#023e8a",
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
