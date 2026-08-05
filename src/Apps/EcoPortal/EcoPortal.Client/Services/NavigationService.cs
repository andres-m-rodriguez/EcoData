using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace EcoPortal.Client.Services;

/// <summary>Direction of the most recent navigation.</summary>
public enum NavigationDirection
{
    /// <summary>No navigation has occurred yet.</summary>
    None,

    /// <summary>Navigated forward to a new page.</summary>
    Forward,

    /// <summary>Navigated back to a previous page.</summary>
    Back
}

/// <summary>Immutable snapshot of the current navigation state.</summary>
/// <param name="Uri">The full URI of the current page.</param>
/// <param name="Path">The path portion of the current URI (without query string).</param>
/// <param name="CanGoBack">Whether back navigation is available.</param>
/// <param name="Direction">The direction of the most recent navigation.</param>
public sealed record NavigationState(
    string Uri,
    string Path,
    bool CanGoBack,
    NavigationDirection Direction);

/// <summary>
/// Tracks a logical back-stack on top of Blazor's <see cref="NavigationManager"/> so the
/// shell can show a back button, animate page transitions by direction, and send deep
/// links "back" to a logical parent. Consumed by MainLayout, PageShell, and tab navigation.
/// </summary>
public sealed class NavigationService : IDisposable
{
    private readonly NavigationManager _nav;
    private readonly IJSRuntime _js;

    private int _depth;
    private string? _parentPath;
    private bool _isNavigatingBack;
    private NavigationDirection _direction = NavigationDirection.None;

    public NavigationService(NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _nav = navigationManager;
        _js = jsRuntime;
        _nav.LocationChanged += OnLocationChanged;

        // Initialize depth - deep links start at depth 1
        var currentPath = GetPathFromUri(_nav.Uri);
        _depth = IsRootPath(currentPath) ? 0 : 1;
    }

    public NavigationState State => new(
        Uri: _nav.Uri,
        Path: GetPathFromUri(_nav.Uri),
        CanGoBack: _depth > 0 || _parentPath is not null,
        Direction: _direction);

    public event Action? OnStateChanged;

    public void NavigateTo(string uri, bool replace = false)
    {
        _parentPath = null; // Clear - will be set by page if needed
        _nav.NavigateTo(uri, replace: replace);
    }

    public async Task GoBackAsync()
    {
        if (_depth > 0)
        {
            _isNavigatingBack = true;
            await _js.InvokeVoidAsync("history.back");
        }
        else if (_parentPath is not null)
        {
            // Deep link - go to logical parent
            _isNavigatingBack = true;
            _nav.NavigateTo(_parentPath);
        }
        // At root with no parent - do nothing
    }

    /// <summary>
    /// Sets the parent path for deep link back navigation. Called by pages that are
    /// accessed via deep links to define where "back" should go.
    /// </summary>
    public void SetParentPath(string? parentPath)
    {
        if (_depth == 0 && _parentPath != parentPath)
        {
            _parentPath = parentPath;
            OnStateChanged?.Invoke();
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var newPath = GetPathFromUri(e.Location);
        var isRoot = IsRootPath(newPath);

        if (_isNavigatingBack)
        {
            _depth = Math.Max(0, _depth - 1);
            _direction = NavigationDirection.Back;
            _isNavigatingBack = false;
        }
        else if (!isRoot)
        {
            // Forward navigation
            _depth++;
            _direction = NavigationDirection.Forward;
        }
        else
        {
            // Navigated to a root path (not back) - reset depth
            _depth = 0;
            _direction = NavigationDirection.Forward;
        }

        _parentPath = null; // Will be set by new page if needed
        OnStateChanged?.Invoke();
    }

    private static bool IsRootPath(string path)
    {
        // A path is considered a "root" if it has no segments or just one segment
        // e.g., "/" or "/monitor" but not "/sensors/123"
        var trimmed = path.TrimStart('/');
        return string.IsNullOrEmpty(trimmed) || !trimmed.Contains('/');
    }

    private static string GetPathFromUri(string uri)
    {
        var uriObj = new Uri(uri);
        return uriObj.AbsolutePath;
    }

    public void Dispose()
    {
        _nav.LocationChanged -= OnLocationChanged;
    }
}
