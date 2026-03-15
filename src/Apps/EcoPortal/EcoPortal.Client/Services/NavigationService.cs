using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace EcoPortal.Client.Services;

public interface INavigationService
{
    string CurrentUri { get; }
    string CurrentPath { get; }
    string? ReturnUrl { get; }

    void NavigateTo(string uri, bool replace = false);
    void NavigateTo(string uri, string returnUrl);
    Task GoBackAsync(string? fallback = null);
    void GoBack(string? fallback = null);

    void SetFallback(string path);
    void SetReturnUrlOrFallback(string fallback);
    bool CanGoBack { get; }

    event Action? OnStateChanged;
}

public sealed class NavigationService : INavigationService, IDisposable
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _jsRuntime;
    private readonly List<string> _history = [];
    private string? _fallbackPath;
    private string? _returnUrl;
    private bool _isNavigatingBack;

    public NavigationService(NavigationManager navigationManager, IJSRuntime jsRuntime)
    {
        _navigationManager = navigationManager;
        _jsRuntime = jsRuntime;
        _navigationManager.LocationChanged += OnLocationChanged;

        // Initialize with current path
        _history.Add(GetPathFromUri(_navigationManager.Uri));
    }

    public string CurrentUri => _navigationManager.Uri;

    public string CurrentPath => GetPathFromUri(_navigationManager.Uri);

    public string? ReturnUrl => _returnUrl;

    public bool CanGoBack => _history.Count > 1 || _fallbackPath is not null;

    public event Action? OnStateChanged;

    public void NavigateTo(string uri, bool replace = false)
    {
        _returnUrl = null; // Clear return URL for regular navigation
        _navigationManager.NavigateTo(uri, replace: replace);
    }

    public void NavigateTo(string uri, string returnUrl)
    {
        _returnUrl = returnUrl;
        _navigationManager.NavigateTo(uri);
    }

    public async Task GoBackAsync(string? fallback = null)
    {
        if (_history.Count > 1)
        {
            // Use browser history for proper back/forward navigation
            _isNavigatingBack = true;
            await _jsRuntime.InvokeVoidAsync("history.back");
        }
        else
        {
            // Fall back to explicit navigation when no history exists
            var fallbackPath = fallback ?? _fallbackPath;
            if (fallbackPath is not null)
            {
                _navigationManager.NavigateTo(fallbackPath);
            }
        }
    }

    public void GoBack(string? fallback = null)
    {
        _ = GoBackAsync(fallback);
    }

    public void SetFallback(string path)
    {
        _fallbackPath = path;
        OnStateChanged?.Invoke();
    }

    public void SetReturnUrlOrFallback(string fallback)
    {
        _fallbackPath = _returnUrl ?? fallback;
        OnStateChanged?.Invoke();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var newPath = GetPathFromUri(e.Location);

        if (_isNavigatingBack)
        {
            // Remove the current page from history when navigating back
            if (_history.Count > 0)
            {
                _history.RemoveAt(_history.Count - 1);
            }
            _isNavigatingBack = false;
        }
        else
        {
            // Add new path to history for forward navigation
            _history.Add(newPath);
        }

        // Reset fallback on navigation
        _fallbackPath = null;

        OnStateChanged?.Invoke();
    }

    private static string GetPathFromUri(string uri)
    {
        var uriObj = new Uri(uri);
        return uriObj.AbsolutePath;
    }

    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
