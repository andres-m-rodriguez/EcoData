namespace EcoData.Ui.Shell.Navbar;

// Match is the path prefix that lights the link; it defaults to Href. "/" only
// matches the root exactly, or every page would light Home.
public sealed record UiNavLink(string Href, string Label, string? Match = null)
{
    public bool IsActive(string currentPath)
    {
        var match = Match ?? Href;

        return match == "/"
            ? currentPath is "/" or ""
            : currentPath.StartsWith(match, StringComparison.OrdinalIgnoreCase);
    }
}
