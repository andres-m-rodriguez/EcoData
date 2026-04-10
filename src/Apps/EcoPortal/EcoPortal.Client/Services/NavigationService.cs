using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace EcoPortal.Client.Services;

public enum NavigationDirection
{
    None,
    Forward,
    Back
}

public enum NavigationTab
{
    Home,
    Monitor,
    Orgs,
    Account
}

public interface INavigationService
{
    // State (read-only)
    string CurrentUri { get; }
    string CurrentPath { get; }
    NavigationTab CurrentTab { get; }
    bool CanGoBack { get; }
    NavigationDirection Direction { get; }

    // Navigation methods - these are the ONLY ways to navigate
    void NavigateTo(string uri);
    void NavigateWithReplace(string uri);
    void NavigateToTab(NavigationTab tab);
    Task GoBackAsync();
    void GoBack();

    // For deep link handling - pages can set their logical parent
    void SetParentPath(string parentPath);

    event Action? OnStateChanged;
}

public sealed class NavigationService : INavigationService, IDisposable
{
    private readonly NavigationManager _nav;  // Private, never exposed
    private readonly IJSRuntime _js;

    private NavigationTab _currentTab;
    private int _depth;                    // 0 = at tab root
    private string? _parentPath;           // For deep link back navigation
    private bool _isNavigatingBack;
    private NavigationDirection _direction = NavigationDirection.None;

    public NavigationService(NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _nav = navigationManager;
        _js = jsRuntime;
        _nav.LocationChanged += OnLocationChanged;

        // Initialize with current path
        var currentPath = GetPathFromUri(_nav.Uri);
        _currentTab = GetTabFromPath(currentPath);
        _depth = IsTabRoot(currentPath) ? 0 : 1; // Deep link starts at depth 1
    }

    public string CurrentUri => _nav.Uri;

    public string CurrentPath => GetPathFromUri(_nav.Uri);

    public NavigationTab CurrentTab => _currentTab;

    public bool CanGoBack => _depth > 0 || _parentPath is not null;

    public NavigationDirection Direction => _direction;

    public event Action? OnStateChanged;

    public void NavigateTo(string uri)
    {
        _parentPath = null;  // Clear - will be set by page if needed
        _nav.NavigateTo(uri);
    }

    public void NavigateWithReplace(string uri)
    {
        // Don't increment depth on replace
        _nav.NavigateTo(uri, replace: true);
    }

    public void NavigateToTab(NavigationTab tab)
    {
        _depth = 0;
        _parentPath = null;
        _currentTab = tab;
        _nav.NavigateTo(GetTabRoot(tab));
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
        // At tab root with no parent - do nothing (button hidden anyway)
    }

    public void GoBack()
    {
        _ = GoBackAsync();
    }

    public void SetParentPath(string parentPath)
    {
        // Called by pages on init for deep link support
        if (_depth == 0)
        {
            _parentPath = parentPath;
            OnStateChanged?.Invoke();
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var newPath = GetPathFromUri(e.Location);
        var newTab = GetTabFromPath(newPath);
        var isTabRoot = IsTabRoot(newPath);

        if (newTab != _currentTab)
        {
            // Tab changed
            _currentTab = newTab;
            _depth = isTabRoot ? 0 : 1;  // Deep link = depth 1
            _direction = NavigationDirection.Forward;
        }
        else if (_isNavigatingBack)
        {
            _depth = Math.Max(0, _depth - 1);
            _direction = NavigationDirection.Back;
            _isNavigatingBack = false;
        }
        else if (!isTabRoot)
        {
            // Forward navigation within tab
            _depth++;
            _direction = NavigationDirection.Forward;
        }
        else
        {
            // Navigated to tab root (not back) - reset depth
            _depth = 0;
            _direction = NavigationDirection.Forward;
        }

        _parentPath = null;  // Will be set by new page if needed
        OnStateChanged?.Invoke();
    }

    private static NavigationTab GetTabFromPath(string path) => path switch
    {
        "/" or "" => NavigationTab.Home,
        _ when path.StartsWith("/monitor") || path.StartsWith("/sensors") || path.StartsWith("/alerts")
            => NavigationTab.Monitor,
        _ when path.StartsWith("/orgs") || path.StartsWith("/organizations") || path.StartsWith("/access-requests")
            => NavigationTab.Orgs,
        _ when path.StartsWith("/account") || path.StartsWith("/login") || path.StartsWith("/register")
            => NavigationTab.Account,
        _ => NavigationTab.Home
    };

    private static string GetTabRoot(NavigationTab tab) => tab switch
    {
        NavigationTab.Home => "/",
        NavigationTab.Monitor => "/monitor",
        NavigationTab.Orgs => "/orgs",
        NavigationTab.Account => "/account",
        _ => "/"
    };

    private static bool IsTabRoot(string path)
    {
        return path is "/" or "" or "/monitor" or "/orgs" or "/account";
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
