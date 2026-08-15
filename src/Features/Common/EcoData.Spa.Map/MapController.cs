
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

    public MapCoordinate Center { get; private set; } = MapCoordinate.PuertoRicoCenter;

    public int Zoom { get; private set; } = 9;

    /// <summary>
    /// Set by the map component while attached; used for calls that need a result back from JS.
    /// </summary>
    internal Func<Task<MapGeolocationResult?>>? GeolocationProvider { get; set; }

    // ===== Events =====
    public event Action? OnMarkersChanged;
    public event Action? OnViewChanged;
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
    public event Action? OnHeatmapChanged;

    // ===== Circle State =====

    internal IReadOnlyList<MapHeatPoint> HeatPoints { get; private set; } = [];
    internal MapHeatmapFilter HeatmapFilter { get; private set; } = MapHeatmapFilter.All;
    internal bool HeatmapVisible { get; private set; }

    // ===== Marker Methods =====

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
        {
            OnMarkersChanged?.Invoke();
        }
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

    // ===== GeoJSON Methods =====

    public void AddGeoJson(MapGeoJson geoJson)
    {
        _geoJsonLayers[geoJson.Id] = geoJson;
        OnGeoJsonChanged?.Invoke();
    }

    public bool RemoveGeoJson(string id)
    {
        var removed = _geoJsonLayers.Remove(id);
        if (removed)
        {
            OnGeoJsonChanged?.Invoke();
        }
        return removed;
    }

    public void ClearGeoJson()
    {
        _geoJsonLayers.Clear();
        OnGeoJsonChanged?.Invoke();
    }

    internal IEnumerable<MapGeoJson> GetGeoJsonLayers() => _geoJsonLayers.Values;

    // ===== View Methods =====

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
        OnViewChanged?.Invoke();
    }

    // ===== Circle Methods =====

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

    // ===== Search Radius Methods =====

    public void ShowSearchRadius(MapCoordinate center, double radiusMeters)
    {
        OnSearchRadiusChanged?.Invoke((center, radiusMeters));
    }

    public void ClearSearchRadius()
    {
        OnSearchRadiusChanged?.Invoke(null);
    }

    // ===== Polygon Draw Methods =====

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

    // ===== Heatmap Methods =====

    public void ShowHeatmap(IReadOnlyList<MapHeatPoint> points, MapHeatmapFilter filter = MapHeatmapFilter.All)
    {
        HeatPoints = points;
        HeatmapFilter = filter;
        HeatmapVisible = true;
        OnHeatmapChanged?.Invoke();
    }

    public void SetHeatmapFilter(MapHeatmapFilter filter)
    {
        HeatmapFilter = filter;
        OnHeatmapChanged?.Invoke();
    }

    public void HideHeatmap()
    {
        HeatmapVisible = false;
        HeatPoints = [];
        OnHeatmapChanged?.Invoke();
    }

    // ===== Geolocation =====

    public async Task<MapGeolocationResult?> GetCurrentPositionAsync()
    {
        return GeolocationProvider is null ? null : await GeolocationProvider();
    }

    // ===== Event Raising (called by component) =====

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
