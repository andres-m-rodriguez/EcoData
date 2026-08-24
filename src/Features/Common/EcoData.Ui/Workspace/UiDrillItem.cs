namespace EcoData.Ui.Workspace;

public sealed record UiDrillItem(string Label, string Description, string Href, int? Count = null, bool Hot = false);
