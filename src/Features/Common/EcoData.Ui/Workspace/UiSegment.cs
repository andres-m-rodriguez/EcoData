namespace EcoData.Ui.Workspace;

// One option of a segmented control. Hot marks a count that wants attention.
public sealed record UiSegment(string Key, string Label, string Href, int? Count = null, bool Hot = false);
