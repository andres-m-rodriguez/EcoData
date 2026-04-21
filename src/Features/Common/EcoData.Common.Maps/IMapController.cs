using EcoData.Common.Maps.Models;

namespace EcoData.Common.Maps;

/// <summary>
/// Controller for programmatic manipulation of a NuiMap.
/// </summary>
/// <typeparam name="TMarker">The type of marker data.</typeparam>
public interface IMapController<TMarker>
{
    // ===== Properties =====

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

    // ===== Marker Methods =====

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

    // ===== GeoJSON Methods =====

    /// <summary>
    /// Add a GeoJSON layer to the map.
    /// </summary>
    void AddGeoJson(MapGeoJson geoJson);

    /// <summary>
    /// Remove a GeoJSON layer by ID.
    /// </summary>
    bool RemoveGeoJson(string id);

    /// <summary>
    /// Clear all GeoJSON layers.
    /// </summary>
    void ClearGeoJson();

    // ===== View Methods =====

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

    // ===== Events (internal, raised by component) =====

    /// <summary>
    /// Fired when markers change.
    /// </summary>
    event Action? OnMarkersChanged;

    /// <summary>
    /// Fired when the view changes.
    /// </summary>
    event Action? OnViewChanged;

    /// <summary>
    /// Fired when GeoJSON layers change.
    /// </summary>
    event Action? OnGeoJsonChanged;

    /// <summary>
    /// Fired when a marker is clicked. Receives the marker index.
    /// </summary>
    event Action<int>? OnMarkerClicked;

    /// <summary>
    /// Fired when the map is clicked. Receives the coordinate.
    /// </summary>
    event Action<MapCoordinate>? OnMapClicked;

    /// <summary>
    /// Fired when a GeoJSON feature is clicked. Receives the layer ID and feature properties as JSON.
    /// </summary>
    event Action<string, string?>? OnGeoJsonClicked;

    // ===== Internal methods for component to raise events =====

    /// <summary>
    /// Called by the component when a marker is clicked.
    /// </summary>
    void RaiseMarkerClicked(int index);

    /// <summary>
    /// Called by the component when the map is clicked.
    /// </summary>
    void RaiseMapClicked(MapCoordinate coordinate);

    /// <summary>
    /// Called by the component when a GeoJSON feature is clicked.
    /// </summary>
    void RaiseGeoJsonClicked(string layerId, string? properties);
}
