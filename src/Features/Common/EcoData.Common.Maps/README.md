# EcoData.Common.Maps (Experimental)

A Blazor component library for interactive maps using Leaflet.js.

> **Note:** This library is experimental and the API may change.

## Installation

1. Add a project reference to `EcoData.Common.Maps`

2. Add Leaflet CSS and JS to your `App.razor` or `index.html`:

```html
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
```

## Usage

### Basic Map with Markers

```razor
@using EcoData.Common.Maps
@using EcoData.Common.Maps.Components
@using EcoData.Common.Maps.Models

<NuiMap TMarker="MyMarker" Controller="_mapController" Height="400px" />

@code {
    private readonly MapController<MyMarker> _mapController = new();

    protected override void OnInitialized()
    {
        _mapController.SetMarkers(new List<MyMarker>
        {
            new() { Coordinate = new MapCoordinate(18.46, -66.10) }
        });
    }
}
```

### Custom Marker Type

Implement `IMapMarker` for your marker data:

```csharp
public class MyMarker : IMapMarker
{
    public required MapCoordinate Coordinate { get; set; }
    public string? PopupContent => $"<b>{Name}</b>";
    public string? TooltipContent => Name;

    public string Name { get; set; } = "";
}
```

### Click Events

Subscribe to controller events:

```csharp
protected override void OnInitialized()
{
    _mapController.OnMarkerClicked += index =>
    {
        var marker = _mapController.Markers[index];
        // Handle marker click
    };

    _mapController.OnMapClicked += coordinate =>
    {
        // Handle map click at coordinate
    };

    _mapController.OnGeoJsonClicked += (layerId, properties) =>
    {
        // Handle GeoJSON feature click
    };
}
```

### GeoJSON Layers

Add polygon/shape overlays:

```csharp
_mapController.AddGeoJson(new MapGeoJson
{
    Id = "my-layer",
    Data = """
    {
        "type": "Feature",
        "geometry": {
            "type": "Polygon",
            "coordinates": [[[-66.0, 18.4], [-65.9, 18.4], [-65.9, 18.3], [-66.0, 18.3], [-66.0, 18.4]]]
        }
    }
    """,
    FillColor = "#3388ff",
    FillOpacity = 0.3,
    StrokeColor = "#3388ff",
    StrokeWidth = 2
});

// Remove layer
_mapController.RemoveGeoJson("my-layer");

// Clear all layers
_mapController.ClearGeoJson();
```

### View Control

```csharp
// Set center and zoom
_mapController.SetView(new MapCoordinate(18.46, -66.10), zoom: 12);

// Fit to show all markers
_mapController.FitToMarkers();

// Fit to specific bounds
_mapController.FitToBounds(new MapBounds(
    southWest: new MapCoordinate(18.0, -67.0),
    northEast: new MapCoordinate(18.5, -65.5)
));
```

## API Reference

### NuiMap Component

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Controller | IMapController<TMarker> | required | Map controller instance |
| Height | string | "400px" | Map height |
| Width | string | "100%" | Map width |
| Class | string? | null | Additional CSS classes |
| Style | string? | null | Additional inline styles |

### MapController Methods

| Method | Description |
|--------|-------------|
| SetMarkers(IEnumerable) | Replace all markers |
| AddMarker(TMarker) | Add a single marker |
| RemoveMarker(TMarker) | Remove a marker |
| ClearMarkers() | Remove all markers |
| AddGeoJson(MapGeoJson) | Add a GeoJSON layer |
| RemoveGeoJson(string) | Remove a GeoJSON layer by ID |
| ClearGeoJson() | Remove all GeoJSON layers |
| SetView(MapCoordinate, int) | Set center and zoom |
| FitToMarkers() | Fit view to all markers |
| FitToBounds(MapBounds) | Fit view to bounds |

### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| OnMarkerClicked | Action<int> | Marker index clicked |
| OnMapClicked | Action<MapCoordinate> | Map clicked at coordinate |
| OnGeoJsonClicked | Action<string, string?> | GeoJSON layer ID and feature properties (JSON) |
