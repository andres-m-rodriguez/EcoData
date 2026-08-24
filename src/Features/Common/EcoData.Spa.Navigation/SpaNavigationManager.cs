using EcoData.Spa.Navigation.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Tempest;

namespace EcoData.Spa.Navigation;

/// <summary>
/// Implementation of <see cref="INavigationManager"/> that wraps Blazor's
/// NavigationManager and announces every state change on the event bus.
/// </summary>
/// <remarks>
/// Named <c>Spa</c>-first to stay unambiguous alongside
/// <see cref="Microsoft.AspNetCore.Components.NavigationManager"/>, which it wraps.
/// </remarks>
internal sealed class SpaNavigationManager : INavigationManager, IDisposable
{
    private readonly NavigationManager _nav;
    private readonly IJSRuntime _js;
    private readonly IEventBus _bus;

    private int _depth;
    private string? _parentPath;
    private bool _isNavigatingBack;
    private NavigationDirection _direction = NavigationDirection.None;

    public SpaNavigationManager(NavigationManager navigationManager, IJSRuntime jsRuntime, IEventBus bus)
    {
        _nav = navigationManager;
        _js = jsRuntime;
        _bus = bus;
        _nav.LocationChanged += OnLocationChanged;

        var currentPath = GetPathFromUri(_nav.Uri);
        _depth = IsRootPath(currentPath) ? 0 : 1;
    }

    public NavigationState State => new(
        Uri: _nav.Uri,
        Path: GetPathFromUri(_nav.Uri),
        CanGoBack: _depth > 0 || _parentPath is not null,
        Direction: _direction);

    public void NavigateTo(string uri, bool replace = false)
    {
        _parentPath = null;
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
            _isNavigatingBack = true;
            _nav.NavigateTo(_parentPath);
        }
    }

    public void SetParentPath(string? parentPath)
    {
        if (_depth == 0 && _parentPath != parentPath)
        {
            _parentPath = parentPath;
            Notify();
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
            _depth++;
            _direction = NavigationDirection.Forward;
        }
        else
        {
            _depth = 0;
            _direction = NavigationDirection.Forward;
        }

        _parentPath = null;
        Notify();
    }

    private void Notify() => _bus.Publish(new NavigationChanged(State));

    private static bool IsRootPath(string path)
    {
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
