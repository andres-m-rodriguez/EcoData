namespace EcoPortal.Client.Services;

public interface ILeafletMapService
{
    ValueTask<ILeafletMapInstance> CreateAsync(string elementId, LeafletMapOptions? options = null);
}

public interface ILeafletMapInstance : IAsyncDisposable
{
    string ElementId { get; }

    ValueTask SetViewAsync(double latitude, double longitude, int? zoom = null);
    ValueTask SetMarkerAsync(double latitude, double longitude);
    ValueTask ClearMarkerAsync();
    ValueTask InvalidateSizeAsync();
    ValueTask<GeolocationResult> GetCurrentLocationAsync();

    void OnClick(Func<MapClickEventArgs, Task> handler);
}

public sealed record GeolocationResult(
    bool Success,
    double? Latitude = null,
    double? Longitude = null,
    string? Error = null
);

public sealed record LeafletMapOptions(
    double? InitialLatitude = null,
    double? InitialLongitude = null,
    int InitialZoom = 9,
    bool ShowMarker = false
);

public sealed record MapClickEventArgs(double Latitude, double Longitude);
