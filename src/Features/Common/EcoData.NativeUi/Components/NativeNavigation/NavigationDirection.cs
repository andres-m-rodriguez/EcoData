namespace EcoData.NativeUi.Components.NativeNavigation;

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
