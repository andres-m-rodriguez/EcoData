using EcoData.Spa.Contracts.Navbar;

namespace EcoData.Spa.Core.Navbar;

/// <summary>
/// Manages the navbar state (title, actions) for SPA shells.
///
/// <para>State changes are announced on the Tempest event bus as
/// <see cref="Contracts.Events.NavbarChanged"/> rather than a C# event — see
/// <see cref="Navigation.INavigationManager"/> for the rationale.</para>
/// </summary>
public interface INavbarManager
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
}
