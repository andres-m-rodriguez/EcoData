namespace EcoData.NativeUi.Components.NativeNavigation;

/// <summary>
/// Immutable snapshot of the current navigation state.
/// </summary>
/// <param name="Uri">The full URI of the current page.</param>
/// <param name="Path">The path portion of the current URI (without query string).</param>
/// <param name="CanGoBack">Whether back navigation is available.</param>
/// <param name="Direction">The direction of the most recent navigation.</param>
public sealed record NavigationState(
    string Uri,
    string Path,
    bool CanGoBack,
    NavigationDirection Direction);
