namespace EcoData.Ui.Icons;

// Custom SVG glyphs in MudBlazor's icon format (24x24 viewBox, path markup
// only). Material has no panel-with-rail glyph, so these are drawn here; they
// came to FaunaFinder from the sibling Harmony app.
public static class UiIcons
{
    private const string PanelFrame =
        """<path d="M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2Zm0 2v14h14V5H5Z"/><path d="M8 5h2v14H8Z"/>""";

    // Panel with a left rail — a side panel toggle at rest.
    public const string Panel = PanelFrame;

    // Panel with a right-pointing chevron: hovering while the panel is closed.
    public const string PanelExpand =
        PanelFrame + """<path d="m13 8.6-1.4 1.4 2 2-2 2 1.4 1.4 3.4-3.4Z"/>""";

    // Panel with a left-pointing chevron: hovering while the panel is open.
    public const string PanelCollapse =
        PanelFrame + """<path d="m15 8.6 1.4 1.4-2 2 2 2-1.4 1.4-3.4-3.4Z"/>""";
}
