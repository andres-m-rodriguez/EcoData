using EcoData.Common.Maps.Models;

namespace EcoData.Common.Maps;

/// <summary>
/// Default implementation of <see cref="IMapController{TMarker}"/>.
/// </summary>
/// <typeparam name="TMarker">The type of marker data.</typeparam>
public class MapController<TMarker> : IMapController<TMarker>
{
    private readonly List<TMarker> _markers = [];

    public IReadOnlyList<TMarker> Markers => _markers;

    public MapCoordinate Center { get; private set; } = MapCoordinate.PuertoRicoCenter;

    public int Zoom { get; private set; } = 9;

    public event Action? OnMarkersChanged;
    public event Action? OnViewChanged;

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
}
