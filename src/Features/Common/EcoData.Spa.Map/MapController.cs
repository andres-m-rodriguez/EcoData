
namespace EcoData.Spa.Map;

/// <summary>
/// Default implementation of <see cref="IMapController{TMarker}"/>.
/// </summary>
/// <typeparam name="TMarker">The type of marker data.</typeparam>
public class MapController<TMarker> : IMapController<TMarker>
{
    private readonly List<TMarker> _markers = [];
    private readonly Dictionary<string, MapGeoJson> _geoJsonLayers = [];
    private readonly List<MapCircle> _circles = [];

    public IReadOnlyList<TMarker> Markers => _markers;

    public IReadOnlyList<MapCircle> Circles => _circles;

    /// <summary>
    /// No default location: this library is region-agnostic, so consumers call
    /// <see cref="SetView"/> during initialization to place the map.
    /// </summary>
    public MapCoordinate Center { get; private set; }

    public int Zoom { get; private set; } = 9;

    public (MapCoordinate Center, double RadiusMeters)? SearchRadius { get; private set; }

    public IReadOnlyList<IReadOnlyList<MapCoordinate>> Polygons { get; private set; } = [];

    /// <summary>
    /// Set by the map component while attached; used for calls that need a result back from JS.
    /// </summary>
    internal Func<Task<MapGeolocationResult?>>? GeolocationProvider { get; set; }

    public event Action? OnMarkersChanged;
    public event Action? OnViewChanged;
    public event Action<MapBounds>? OnFitBoundsRequested;
    public event Action? OnGeoJsonChanged;
    public event Action<int>? OnMarkerClicked;
    public event Action<MapCoordinate>? OnMapClicked;
    public event Action<string, string?>? OnGeoJsonClicked;
    public event Action<string, bool>? OnGeoJsonLoaded;
    public event Action? OnCirclesChanged;
    public event Action<int>? OnCircleClicked;
    public event Action<int?>? OnCircleFocusRequested;
    public event Action<(MapCoordinate Center, double RadiusMeters)?>? OnSearchRadiusChanged;
    public event Action<MapPolygonDrawAction>? OnPolygonDrawActionRequested;
    public event Action<IReadOnlyList<MapCoordinate>>? OnPolygonDrawn;
    public event Action? OnPolygonDrawCancelled;
    public event Action? OnPolygonsChanged;

    public void SetMarkers(IEnumerable<TMarker> markers)
    {
        _markers.Clear();
        _markers.AddRange(markers);
        OnMarkersChanged?.Invoke();
    }

    public void AddMarker(TMarker marker)
    {
        _markers.Add(marker);
        OnMarkersChanged?.Invoke();
    }

    public bool RemoveMarker(TMarker marker)
    {
        var removed = _markers.Remove(marker);
        if (removed)
            OnMarkersChanged?.Invoke();
        return removed;
    }

    public void ClearMarkers()
    {
        _markers.Clear();
        OnMarkersChanged?.Invoke();
    }

    public void UpdateMarker(TMarker marker)
    {
        var index = _markers.IndexOf(marker);
        if (index >= 0)
        {
            _markers[index] = marker;
            OnMarkersChanged?.Invoke();
        }
    }

    public int IndexOf(TMarker marker) => _markers.IndexOf(marker);

    public void AddGeoJson(MapGeoJson geoJson)
    {
        _geoJsonLayers[geoJson.Id] = geoJson;
        OnGeoJsonChanged?.Invoke();
    }

    public bool RemoveGeoJson(string id)
    {
        var removed = _geoJsonLayers.Remove(id);
        if (removed)
            OnGeoJsonChanged?.Invoke();
        return removed;
    }

    public void ClearGeoJson()
    {
        _geoJsonLayers.Clear();
        OnGeoJsonChanged?.Invoke();
    }

    internal IEnumerable<MapGeoJson> GetGeoJsonLayers() => _geoJsonLayers.Values;

    public void SetView(MapCoordinate center, int zoom)
    {
        Center = center;
        Zoom = zoom;
        OnViewChanged?.Invoke();
    }

    public void FitToMarkers()
    {
        OnViewChanged?.Invoke();
    }

    public void FitToBounds(MapBounds bounds)
    {
        OnFitBoundsRequested?.Invoke(bounds);
    }

    public void SetCircles(IEnumerable<MapCircle> circles)
    {
        _circles.Clear();
        _circles.AddRange(circles);
        OnCirclesChanged?.Invoke();
    }

    public void ClearCircles()
    {
        _circles.Clear();
        OnCirclesChanged?.Invoke();
    }

    public void FocusCircle(int index)
    {
        OnCircleFocusRequested?.Invoke(index);
    }

    public void FocusAllCircles()
    {
        OnCircleFocusRequested?.Invoke(null);
    }

    public void ShowSearchRadius(MapCoordinate center, double radiusMeters)
    {
        SearchRadius = (center, radiusMeters);
        OnSearchRadiusChanged?.Invoke((center, radiusMeters));
    }

    public void ClearSearchRadius()
    {
        SearchRadius = null;
        OnSearchRadiusChanged?.Invoke(null);
    }

    public void EnablePolygonDraw()
    {
        OnPolygonDrawActionRequested?.Invoke(MapPolygonDrawAction.Enable);
    }

    public void FinishPolygonDraw()
    {
        OnPolygonDrawActionRequested?.Invoke(MapPolygonDrawAction.Finish);
    }

    public void CancelPolygonDraw()
    {
        OnPolygonDrawActionRequested?.Invoke(MapPolygonDrawAction.Cancel);
    }

    public void ClearDrawnPolygon()
    {
        OnPolygonDrawActionRequested?.Invoke(MapPolygonDrawAction.ClearDrawn);
    }

    public void ShowPolygons(IReadOnlyList<IReadOnlyList<MapCoordinate>> polygons)
    {
        Polygons = polygons;
        OnPolygonsChanged?.Invoke();
    }

    public void ClearPolygons()
    {
        Polygons = [];
        OnPolygonsChanged?.Invoke();
    }

    public async Task<MapGeolocationResult?> GetCurrentPositionAsync()
    {
        return GeolocationProvider is null ? null : await GeolocationProvider();
    }

    public void RaiseMarkerClicked(int index)
    {
        OnMarkerClicked?.Invoke(index);
    }

    public void RaiseMapClicked(MapCoordinate coordinate)
    {
        OnMapClicked?.Invoke(coordinate);
    }

    public void RaiseGeoJsonClicked(string layerId, string? properties)
    {
        OnGeoJsonClicked?.Invoke(layerId, properties);
    }

    public void RaiseGeoJsonLoaded(string layerId, bool success)
    {
        OnGeoJsonLoaded?.Invoke(layerId, success);
    }

    public void RaiseCircleClicked(int index)
    {
        OnCircleClicked?.Invoke(index);
    }

    public void RaisePolygonDrawn(IReadOnlyList<MapCoordinate> coordinates)
    {
        OnPolygonDrawn?.Invoke(coordinates);
    }

    public void RaisePolygonDrawCancelled()
    {
        OnPolygonDrawCancelled?.Invoke();
    }
}
