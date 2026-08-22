namespace EcoData.Ui.Workspace;

// One destination in a workspace: a rail row on desktop, a chip on a phone.
// Count is quiet context ("12/14"); Badge is something waiting on the reader.
public sealed record UiRailLink(
    string Key,
    string Label,
    string Href,
    string Icon,
    string? Count = null,
    int Badge = 0
);
