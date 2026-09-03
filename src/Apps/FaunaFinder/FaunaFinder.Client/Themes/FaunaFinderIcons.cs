namespace FaunaFinder.Client.Themes;

public static class FaunaFinderIcons
{
    private const string PanelFrame =
        """<path d="M5 3h14a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2Zm0 2v14h14V5H5Z"/><path d="M8 5h2v14H8Z"/>""";

    public const string RailPanel = PanelFrame;

    public const string RailExpand =
        PanelFrame + """<path d="m13 8.6-1.4 1.4 2 2-2 2 1.4 1.4 3.4-3.4Z"/>""";

    public const string RailCollapse =
        PanelFrame + """<path d="m15 8.6 1.4 1.4-2 2 2 2-1.4 1.4-3.4-3.4Z"/>""";
}
