using EcoData.Common.Maps.Models;

namespace EcoData.Common.Maps;

/// <summary>
/// Default implementation of <see cref="IMapController{TMarker}"/>.
/// </summary>
/// <typeparam name="TMarker">The type of marker data.</typeparam>
public class MapController<TMarker> : IMapController<TMarker>
{
    private readonly List<TMarker> _markers = [];
    private readonly Dictionary<string, MapGeoJson> _geoJsonLayers = [];

    public IReadOnlyList<TMarker> Markers => _markers;

    public MapCoordinate Center { get; private set; } = MapCoordinate.PuertoRicoCenter;

    public int Zoom { get; private set; } = 9;

    // ===== Events =====
    public event Action? OnMarkersChanged;
    public event Action? OnViewChanged;
    public event Action? OnGeoJsonChanged;
    public event Action<int>? OnMarkerClicked;
    public event Action<MapCoordinate>? OnMapClicked;
    public event Action<string, string?>? OnGeoJsonClicked;

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
}
