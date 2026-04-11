namespace EcoData.NativeUi.Services;

/// <summary>
/// Direction of the most recent navigation.
/// </summary>
public enum NavigationDirection
{
    /// <summary>No navigation has occurred yet.</summary>
    None,

    /// <summary>Navigated forward to a new page.</summary>
    Forward,

    /// <summary>Navigated back to a previous page.</summary>
    Back
}

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
