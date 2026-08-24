using MudBlazor;

namespace FaunaFinder.Client.Themes;

public static class FaunaFinderTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1f4d3a",
            PrimaryContrastText = "#ffffff",
            PrimaryDarken = "#163b2c",
            PrimaryLighten = "#3f7d5f",
            Secondary = "#3f7d5f",
            SecondaryContrastText = "#ffffff",
            Tertiary = "#8a6f3e",
            TertiaryContrastText = "#ffffff",
            Info = "#0077b6",
            InfoLighten = "#caf0f8",
            Success = "#16A34A",
            SuccessLighten = "#F0FDF4",
            Warning = "#D97706",
            WarningLighten = "#FFFBEB",
            Error = "#ba1a1a",
            ErrorLighten = "#ffdad6",
            AppbarBackground = "#1f4d3a",
            AppbarText = "#ffffff",
            DrawerBackground = "#f4f4ef",
            DrawerText = "#163b2c",
            Background = "#f4f4ef",
            BackgroundGray = "#eceae1",
            Surface = "#ffffff",
            TextPrimary = "#1a1c1a",
            TextSecondary = "#414844",
            TextDisabled = "#717973",
            Divider = "#e5e3dc",
            LinesDefault = "#e5e3dc",
            LinesInputs = "#c1c8c2",
            ActionDefault = "#717973",
            ActionDisabled = "#c1c8c2",
            HoverOpacity = 0.04,
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#5a9b7a",
            PrimaryContrastText = "#163b2c",
            PrimaryDarken = "#3f7d5f",
            PrimaryLighten = "#86b8a0",
            Secondary = "#86b8a0",
            SecondaryContrastText = "#163b2c",
            Tertiary = "#c8a670",
            TertiaryContrastText = "#2f1500",
            Info = "#90e0ef",
            InfoLighten = "#023e8a",
            Success = "#4ADE80",
            SuccessLighten = "#14532D",
            Warning = "#FBBF24",
            WarningLighten = "#451A03",
            Error = "#ffb4ab",
            ErrorLighten = "#93000a",
            AppbarBackground = "#1c1c1e",
            AppbarText = "#f3f2f1",
            DrawerBackground = "#1c1c1e",
            DrawerText = "#f3f2f1",
            Background = "#121212",
            BackgroundGray = "#1c1c1e",
            Surface = "#1c1c1e",
            TextPrimary = "#e1e3e2",
            TextSecondary = "#c1c8c2",
            TextDisabled = "#717973",
            Divider = "#414844",
            LinesDefault = "#414844",
            LinesInputs = "#717973",
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
