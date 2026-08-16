namespace EcoData.Spa.Navigation;

/// <summary>
/// The paths an app treats as navigation roots — the destinations its bottom
/// nav or tab bar owns. Landing on one resets the back stack; anything else is
/// a page you can go back from.
///
/// <para>Leave <see cref="RootPaths"/> empty and the manager falls back to
/// calling any single-segment path a root. That guess is wrong for any app
/// whose sections link sideways — FaunaFinder's Browse hub links to
/// <c>/categories</c>, <c>/practices</c> and <c>/actions</c>, all one segment,
/// so every one of them looked like a root and offered no way back.</para>
/// </summary>
public sealed class SpaNavigationOptions
{
    /// <summary>
    /// Root paths, compared case-insensitively with trailing slashes ignored.
    /// Empty means "use the single-segment heuristic".
    /// </summary>
    public IReadOnlyList<string> RootPaths { get; init; } = [];
}
