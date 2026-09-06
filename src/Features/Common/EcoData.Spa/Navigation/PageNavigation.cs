using System.Text.Json;
using EcoData.Spa.Interop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Tempest;

namespace EcoData.Spa.Navigation;

public sealed class PageNavigation(NavigationManager navigation, IJavascriptSafeInterop js, IEventBus bus) : IDisposable
{
    private const string StorageKey = "ecodata.nav";

    private readonly List<string> _stack = [];
    private readonly HashSet<string> _recorded = [];
    private EventHandler<LocationChangedEventArgs>? _onLocationChanged;
    private string? _current;
    private string? _parentPath;
    private bool _navigatingBack;

    public bool CanGoBack => _stack.Count > 0 || _parentPath is not null;

    public void Record() => _recorded.Add(navigation.ToBaseRelativePath(navigation.Uri));
    public void SetParentPath(string? parentPath)
    {
        if (_parentPath == parentPath)
            return;

        _parentPath = parentPath;
        bus.Publish<NavigationChanged>();
    }
    public async Task InitializeAsync()
    {
        if (_onLocationChanged is not null)
            return;

        _current = navigation.ToBaseRelativePath(navigation.Uri);

        var stored = await js.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
        if (stored.TryPickT0(out var json, out _) && json is not null)
            _stack.AddRange(JsonSerializer.Deserialize<List<string>>(json) ?? []);

        // A restored top equal to the page we landed on means the user refreshed right
        // after a back navigation; drop it so back does not loop in place.
        if (_stack.Count > 0 && _stack[^1] == _current)
            _stack.RemoveAt(_stack.Count - 1);

        _onLocationChanged = (_, e) =>
        {
            var to = navigation.ToBaseRelativePath(e.Location);
            var from = _current!;
            // Consume the leaving page's opt-in; an early Record() by the destination stays.
            var fromRecorded = _recorded.Remove(from);

            if (_navigatingBack)
                _navigatingBack = false;
            else if (_stack.Count > 0 && _stack[^1] == to)
                _stack.RemoveAt(_stack.Count - 1); // the browser's own back button
            else if (fromRecorded && from != to)
                _stack.Add(from);
            else
                _stack.Clear(); // left a page that never opted in, so the chain is stale

            _current = to;
            _parentPath = null;
            Persist(js, _stack);
            bus.Publish<NavigationChanged>();
        };
        navigation.LocationChanged += _onLocationChanged;
        bus.Publish<NavigationChanged>();
    }

    public void GoBack()
    {
        if (_stack.Count > 0)
        {
            var target = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            _navigatingBack = true;
            Persist(js, _stack);
            navigation.NavigateTo(target);
            return;
        }

        if (_parentPath is null)
            return;

        _navigatingBack = true;
        navigation.NavigateTo(_parentPath);
    }

    public void Reset()
    {
        _stack.Clear();
        _recorded.Clear();
        Persist(js, _stack);
        bus.Publish<NavigationChanged>();
    }

    public void Dispose() => navigation.LocationChanged -= _onLocationChanged;

    private static void Persist(IJavascriptSafeInterop js, List<string> stack) =>
        _ = js.InvokeVoidAsync("sessionStorage.setItem", StorageKey, JsonSerializer.Serialize(stack));
}
