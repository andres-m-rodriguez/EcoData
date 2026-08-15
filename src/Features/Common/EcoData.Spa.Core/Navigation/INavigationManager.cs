using EcoData.Spa.Contracts.Navigation;

namespace EcoData.Spa.Core.Navigation;

/// <summary>
/// Platform-agnostic navigation manager for SPA shells. Provides a DTO-driven
/// API for navigation state and operations.
///
/// <para>State changes are announced on the Tempest event bus as
/// <see cref="Contracts.Events.NavigationChanged"/> rather than a C# event, so
/// subscribers get automatic teardown and a marshalled re-render from
/// <c>[Event]</c> instead of hand-wiring <c>+=</c>/<c>-=</c>.</para>
/// </summary>
public interface INavigationManager
{
    /// <summary>
    /// Gets the current navigation state as an immutable snapshot.
    /// </summary>
    NavigationState State { get; }

    /// <summary>
    /// Navigates to the specified URI.
    /// </summary>
    /// <param name="uri">The URI to navigate to.</param>
    /// <param name="replace">If true, replaces the current history entry instead of adding a new one.</param>
    void NavigateTo(string uri, bool replace = false);

    /// <summary>
    /// Navigates back to the previous page asynchronously.
    /// Uses browser history when available, or the parent path for deep links.
    /// </summary>
    Task GoBackAsync();

    /// <summary>
    /// Sets the parent path for deep link back navigation.
    /// Called by pages that are accessed via deep links to define where "back" should go.
    /// </summary>
    /// <param name="parentPath">The path to navigate to when going back from a deep link.</param>
    void SetParentPath(string? parentPath);
}
