// Leaflet map interop for NuiMap component

export function initialize(element, lat, lng, zoom, dotNetRef) {
    const map = L.map(element).setView([lat, lng], zoom);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    map._nuiMarkers = L.layerGroup().addTo(map);
    map._nuiGeoJson = L.layerGroup().addTo(map);
    map._dotNetRef = dotNetRef;

    // Map click handler
    map.on('click', (e) => {
        if (map._dotNetRef) {
            map._dotNetRef.invokeMethodAsync('OnMapClickedFromJs', e.latlng.lat, e.latlng.lng);
        }
    });

    return map;
}

export function setView(map, lat, lng, zoom) {
    if (map) {
        map.setView([lat, lng], zoom);
    }
}

export function setMarkers(map, markers) {
    if (!map) return;

    map._nuiMarkers.clearLayers();

    markers.forEach(m => {
        const marker = L.marker([m.lat, m.lng]);

        if (m.popup) {
            marker.bindPopup(m.popup);
        }

        if (m.tooltip) {
            marker.bindTooltip(m.tooltip);
        }

        // Marker click handler
        marker.on('click', () => {
            if (map._dotNetRef) {
                map._dotNetRef.invokeMethodAsync('OnMarkerClickedFromJs', m.index);
            }
        });

        map._nuiMarkers.addLayer(marker);
    });
}

export function setGeoJson(map, layers) {
    if (!map) return;

    map._nuiGeoJson.clearLayers();

    layers.forEach(layer => {
        try {
            const geoJsonData = JSON.parse(layer.data);
            const geoJsonLayer = L.geoJSON(geoJsonData, {
                style: {
                    fillColor: layer.fillColor,
                    fillOpacity: layer.fillOpacity,
                    color: layer.strokeColor,
                    weight: layer.strokeWidth
                },
                onEachFeature: (feature, leafletLayer) => {
                    // GeoJSON feature click handler
                    leafletLayer.on('click', (e) => {
                        L.DomEvent.stopPropagation(e);
                        if (map._dotNetRef) {
                            const properties = feature.properties
                                ? JSON.stringify(feature.properties)
                                : null;
                            map._dotNetRef.invokeMethodAsync('OnGeoJsonClickedFromJs', layer.id, properties);
                        }
                    });
                }
            });
            map._nuiGeoJson.addLayer(geoJsonLayer);
        } catch (e) {
            console.error('Failed to parse GeoJSON for layer:', layer.id, e);
        }
    });
}

export function fitToMarkers(map) {
    if (!map) return;

    const layers = map._nuiMarkers.getLayers();
    if (layers.length > 0) {
        const group = L.featureGroup(layers);
        map.fitBounds(group.getBounds(), { padding: [20, 20] });
    }
}

export function fitToBounds(map, southWestLat, southWestLng, northEastLat, northEastLng) {
    if (!map) return;

    const bounds = L.latLngBounds(
        [southWestLat, southWestLng],
        [northEastLat, northEastLng]
    );
    map.fitBounds(bounds);
}

export function dispose(map) {
    if (map) {
        map.remove();
    }
}
