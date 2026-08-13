namespace FaunaFinder.Client.Themes;

/// <summary>
/// Custom SVG glyphs (24x24 viewBox, MudBlazor icon format) for the field
/// notebook toggle: a plain panel at rest, and the same panel with an
/// expand/collapse chevron on hover.
///
/// <para>Ported from the sibling Harmony app's <c>HarmonyIcons</c>, which
/// drew them for the same job — Material has no panel-with-rail glyph.</para>
/// </summary>
public static class FaunaFinderIcons
{
    private const string PanelFrame =
        """<path d="M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2Zm0 2v14h14V5H5Z"/><path d="M8 5h2v14H8Z"/>""";

    /// <summary>Panel with a left rail — the resting notebook toggle.</summary>
    public const string NotebookPanel = PanelFrame;

    /// <summary>Panel with a right-pointing chevron: hovering while the notebook is closed.</summary>
    public const string NotebookExpand =
        PanelFrame + """<path d="m13 8.6-1.4 1.4 2 2-2 2 1.4 1.4 3.4-3.4Z"/>""";

    /// <summary>Panel with a left-pointing chevron: hovering while the notebook is open.</summary>
    public const string NotebookCollapse =
        PanelFrame + """<path d="m15 8.6 1.4 1.4-2 2 2 2-1.4 1.4-3.4-3.4Z"/>""";
}
