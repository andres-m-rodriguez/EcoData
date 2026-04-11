namespace EcoData.NativeUi.Components.NativeNavbar;

/// <summary>
/// Manages the navbar state (title, actions) for native UI applications.
/// </summary>
public interface INativeNavbarManager
{
    /// <summary>
    /// Gets the current navbar state including title and actions.
    /// </summary>
    NavbarState State { get; }

    /// <summary>
    /// Sets the complete navbar state at once.
    /// </summary>
    void SetState(NavbarState state);

    /// <summary>
    /// Sets the page title displayed in the navbar.
    /// </summary>
    void SetTitle(string? title);

    /// <summary>
    /// Sets the action buttons displayed in the navbar.
    /// </summary>
    void SetActions(params NavbarAction[] actions);

    /// <summary>
    /// Clears all action buttons from the navbar.
    /// </summary>
    void ClearActions();

    /// <summary>
    /// Resets the navbar to its default state (no title, no actions).
    /// </summary>
    void Reset();

    /// <summary>
    /// Raised when the navbar state changes.
    /// </summary>
    event Action? OnStateChanged;
}
