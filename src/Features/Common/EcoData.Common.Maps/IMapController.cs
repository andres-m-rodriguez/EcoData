using EcoData.Common.Maps.Models;

namespace EcoData.Common.Maps;

/// <summary>
/// Controller for programmatic manipulation of a NuiMap.
/// </summary>
/// <typeparam name="TMarker">The type of marker data.</typeparam>
public interface IMapController<TMarker>
{
    /// <summary>
    /// Current markers on the map.
    /// </summary>
    IReadOnlyList<TMarker> Markers { get; }

    /// <summary>
    /// Current map center coordinate.
    /// </summary>
    MapCoordinate Center { get; }

    /// <summary>
    /// Current zoom level.
    /// </summary>
    int Zoom { get; }

    /// <summary>
    /// Set all markers.
    /// </summary>
    void SetMarkers(IEnumerable<TMarker> markers);

    /// <summary>
    /// Add a marker to the map.
    /// </summary>
    void AddMarker(TMarker marker);

    /// <summary>
    /// Remove a marker from the map.
    /// </summary>
    bool RemoveMarker(TMarker marker);

    /// <summary>
    /// Clear all markers.
    /// </summary>
    void ClearMarkers();

    /// <summary>
    /// Update a marker (triggers re-render).
    /// </summary>
    void UpdateMarker(TMarker marker);

    /// <summary>
    /// Get the index of a marker.
    /// </summary>
    int IndexOf(TMarker marker);

    /// <summary>
    /// Set the map view (center and zoom).
    /// </summary>
    void SetView(MapCoordinate center, int zoom);

    /// <summary>
    /// Fit the map to show all markers.
    /// </summary>
    void FitToMarkers();

    /// <summary>
    /// Fit the map to specific bounds.
    /// </summary>
    void FitToBounds(MapBounds bounds);

    /// <summary>
    /// Fired when markers change.
    /// </summary>
    event Action? OnMarkersChanged;

    /// <summary>
    /// Fired when the view changes.
    /// </summary>
    event Action? OnViewChanged;
}
