namespace EcoData.NativeUi.Components.NativeNavigation;

/// <summary>
/// Platform-agnostic navigation manager for native UI applications.
/// Provides a clean, DTO-driven API for navigation state and operations.
/// </summary>
public interface INativeNavigationManager
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

    /// <summary>
    /// Raised when the navigation state changes.
    /// </summary>
    event Action? OnStateChanged;
}
