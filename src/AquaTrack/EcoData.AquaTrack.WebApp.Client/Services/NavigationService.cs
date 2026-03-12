using System.Web;
using BlazorStaticNavigation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace EcoData.AquaTrack.WebApp.Client.Services;

public interface INavigationService
{
    string CurrentUri { get; }
    string CurrentPath { get; }

    void NavigateTo(string uri);
    void NavigateTo<TPage>() where TPage : INavigablePage<TPage>;
    void NavigateTo<TPage, TParams>(TParams parameters) where TPage : INavigablePage<TPage, TParams>;
    void GoBack(string? fallback = null);

    void SetFallback(string path);
    void SetFallback<TPage>() where TPage : INavigablePage<TPage>;
    bool CanGoBack { get; }

    event Action? OnStateChanged;
}

public sealed class NavigationService : INavigationService, IDisposable
{
    private readonly NavigationManager _navigationManager;
    private string? _fallbackPath;

    public NavigationService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
        _navigationManager.LocationChanged += OnLocationChanged;
    }

    public string CurrentUri => _navigationManager.Uri;

    public string CurrentPath
    {
        get
        {
            var uri = new Uri(_navigationManager.Uri);
            return uri.AbsolutePath;
        }
    }

    public bool CanGoBack => _fallbackPath is not null && !CurrentPath.Equals(_fallbackPath, StringComparison.OrdinalIgnoreCase);

    public event Action? OnStateChanged;

    public void NavigateTo(string uri)
    {
        _navigationManager.NavigateTo(uri);
    }

    public void NavigateTo<TPage>() where TPage : INavigablePage<TPage>
    {
        _navigationManager.NavigateTo(TPage.Path);
    }

    public void NavigateTo<TPage, TParams>(TParams parameters) where TPage : INavigablePage<TPage, TParams>
    {
        _navigationManager.NavigateTo(TPage.GetPathWithParameters(parameters));
    }

    public void GoBack(string? fallback = null)
    {
        var target = fallback ?? _fallbackPath;
        if (target is not null)
        {
            _navigationManager.NavigateTo(target);
        }
    }

    public void SetFallback(string path)
    {
        _fallbackPath = path;
        OnStateChanged?.Invoke();
    }

    public void SetFallback<TPage>() where TPage : INavigablePage<TPage>
    {
        SetFallback(TPage.Path);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        OnStateChanged?.Invoke();
    }

    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
