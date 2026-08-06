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

    // ===== Circle Methods =====

    /// <summary>
    /// Current circles on the map.
    /// </summary>
    IReadOnlyList<MapCircle> Circles { get; }

    /// <summary>
    /// Replace all circles on the map.
    /// </summary>
    void SetCircles(IEnumerable<MapCircle> circles);

    /// <summary>
    /// Remove all circles.
    /// </summary>
    void ClearCircles();

    /// <summary>
    /// Fly to a circle by index and open its popup.
    /// </summary>
    void FocusCircle(int index);

    /// <summary>
    /// Fly to bounds containing every circle.
    /// </summary>
    void FocusAllCircles();

    // ===== Search Radius Methods =====

    /// <summary>
    /// Show the user-location dot and a dashed search-radius circle, and fly to it.
    /// </summary>
    void ShowSearchRadius(MapCoordinate center, double radiusMeters);

    /// <summary>
    /// Remove the search-radius overlay.
    /// </summary>
    void ClearSearchRadius();

    // ===== Polygon Draw Methods =====

    /// <summary>
    /// Enter polygon draw mode. Vertices are added by clicking; the shape completes on
    /// double-click, Enter, or clicking the first vertex, and fires <see cref="OnPolygonDrawn"/>.
    /// </summary>
    void EnablePolygonDraw();

    /// <summary>
    /// Complete the polygon being drawn (requires at least 3 vertices).
    /// </summary>
    void FinishPolygonDraw();

    /// <summary>
    /// Cancel draw mode and remove any in-progress shape.
    /// </summary>
    void CancelPolygonDraw();

    /// <summary>
    /// Remove a completed drawn polygon from the map.
    /// </summary>
    void ClearDrawnPolygon();

    // ===== Heatmap Methods =====

    /// <summary>
    /// Show (or refresh) the heatmap layer with the given points.
    /// </summary>
    void ShowHeatmap(IReadOnlyList<MapHeatPoint> points, MapHeatmapFilter filter = MapHeatmapFilter.All);

    /// <summary>
    /// Re-filter the currently shown heatmap.
    /// </summary>
    void SetHeatmapFilter(MapHeatmapFilter filter);

    /// <summary>
    /// Remove the heatmap layer.
    /// </summary>
    void HideHeatmap();

    // ===== Geolocation =====

    /// <summary>
    /// Ask the browser for the user's current position. Returns null when no map
    /// component is attached yet.
    /// </summary>
    Task<MapGeolocationResult?> GetCurrentPositionAsync();

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

    /// <summary>
    /// Fired when circles change.
    /// </summary>
    event Action? OnCirclesChanged;

    /// <summary>
    /// Fired when a circle is clicked. Receives the circle index.
    /// </summary>
    event Action<int>? OnCircleClicked;

    /// <summary>
    /// Fired when a circle focus is requested. Receives the circle index, or null for all circles.
    /// </summary>
    event Action<int?>? OnCircleFocusRequested;

    /// <summary>
    /// Fired when the search radius overlay changes. Receives null when cleared.
    /// </summary>
    event Action<(MapCoordinate Center, double RadiusMeters)?>? OnSearchRadiusChanged;

    /// <summary>
    /// Fired when a polygon draw action is requested by the caller.
    /// </summary>
    event Action<MapPolygonDrawAction>? OnPolygonDrawActionRequested;

    /// <summary>
    /// Fired when the user completes a polygon. Receives the vertices.
    /// </summary>
    event Action<IReadOnlyList<MapCoordinate>>? OnPolygonDrawn;

    /// <summary>
    /// Fired when the user cancels polygon drawing (Escape).
    /// </summary>
    event Action? OnPolygonDrawCancelled;

    /// <summary>
    /// Fired when the heatmap state changes.
    /// </summary>
    event Action? OnHeatmapChanged;

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

    /// <summary>
    /// Called by the component when a circle is clicked.
    /// </summary>
    void RaiseCircleClicked(int index);

    /// <summary>
    /// Called by the component when the user completes a polygon.
    /// </summary>
    void RaisePolygonDrawn(IReadOnlyList<MapCoordinate> coordinates);

    /// <summary>
    /// Called by the component when the user cancels polygon drawing.
    /// </summary>
    void RaisePolygonDrawCancelled();
}

/// <summary>
/// Polygon draw actions a controller can request from the map component.
/// </summary>
public enum MapPolygonDrawAction
{
    Enable,
    Finish,
    Cancel,
    ClearDrawn,
}
