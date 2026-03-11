using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace EcoData.AquaTrack.WebApp.Client.Services;

public interface INavigationService
{
    string CurrentUri { get; }
    string CurrentPath { get; }

    void NavigateTo(string uri, bool trackReturn = false);
    void GoBack(string? fallback = null);
    string? GetReturnUrl();

    void SetFallback(string path);
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

    public bool CanGoBack => GetReturnUrl() is not null || _fallbackPath is not null;

    public event Action? OnStateChanged;

    public void NavigateTo(string uri, bool trackReturn = false)
    {
        if (trackReturn)
        {
            var returnUrl = Uri.EscapeDataString(CurrentPath);
            var separator = uri.Contains('?') ? "&" : "?";
            uri = $"{uri}{separator}returnUrl={returnUrl}";
        }

        _navigationManager.NavigateTo(uri);
    }

    public void GoBack(string? fallback = null)
    {
        var returnUrl = GetReturnUrl() ?? fallback ?? _fallbackPath;
        if (returnUrl is not null)
        {
            _navigationManager.NavigateTo(returnUrl);
        }
    }

    public string? GetReturnUrl()
    {
        var uri = new Uri(_navigationManager.Uri);
        var query = HttpUtility.ParseQueryString(uri.Query);
        var returnUrl = query["returnUrl"];

        return string.IsNullOrEmpty(returnUrl) ? null : returnUrl;
    }

    public void SetFallback(string path)
    {
        _fallbackPath = path;
        OnStateChanged?.Invoke();
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
