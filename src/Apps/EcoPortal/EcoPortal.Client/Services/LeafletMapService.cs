using Microsoft.JSInterop;

namespace EcoPortal.Client.Services;

public sealed class LeafletMapService(IJSRuntime js) : ILeafletMapService
{
    public async ValueTask<ILeafletMapInstance> CreateAsync(string elementId, LeafletMapOptions? options = null)
    {
        var instance = new LeafletMapInstance(js, elementId);
        await instance.InitializeAsync(options ?? new LeafletMapOptions());
        return instance;
    }
}

public sealed class LeafletMapInstance : ILeafletMapInstance
{
    private readonly IJSRuntime _js;
    private readonly DotNetObjectReference<LeafletMapInstance> _dotNetRef;
    private Func<MapClickEventArgs, Task>? _clickHandler;
    private bool _disposed;

    public string ElementId { get; }

    public LeafletMapInstance(IJSRuntime js, string elementId)
    {
        _js = js;
        ElementId = elementId;
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    internal async ValueTask InitializeAsync(LeafletMapOptions options)
    {
        await _js.InvokeVoidAsync(
            "leafletMapService.create",
            ElementId,
            _dotNetRef,
            options.InitialLatitude,
            options.InitialLongitude,
            options.InitialZoom,
            options.ShowMarker
        );
    }

    public async ValueTask SetViewAsync(double latitude, double longitude, int? zoom = null)
    {
        if (_disposed) return;
        await _js.InvokeVoidAsync("leafletMapService.setView", ElementId, latitude, longitude, zoom);
    }

    public async ValueTask SetMarkerAsync(double latitude, double longitude)
    {
        if (_disposed) return;
        await _js.InvokeVoidAsync("leafletMapService.setMarker", ElementId, latitude, longitude);
    }

    public async ValueTask ClearMarkerAsync()
    {
        if (_disposed) return;
        await _js.InvokeVoidAsync("leafletMapService.clearMarker", ElementId);
    }

    public async ValueTask InvalidateSizeAsync()
    {
        if (_disposed) return;
        await _js.InvokeVoidAsync("leafletMapService.invalidateSize", ElementId);
    }

    public void OnClick(Func<MapClickEventArgs, Task> handler)
    {
        _clickHandler = handler;
    }

    [JSInvokable]
    public async Task HandleMapClick(double latitude, double longitude)
    {
        if (_clickHandler is not null)
        {
            await _clickHandler(new MapClickEventArgs(latitude, longitude));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            await _js.InvokeVoidAsync("leafletMapService.dispose", ElementId);
        }
        catch (JSDisconnectedException)
        {
            // Ignore during app shutdown
        }

        _dotNetRef.Dispose();
    }
}
