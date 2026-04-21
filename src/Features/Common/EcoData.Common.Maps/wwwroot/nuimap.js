// Leaflet map interop for NuiMap component

export function initialize(element, lat, lng, zoom) {
    const map = L.map(element).setView([lat, lng], zoom);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    map._nuiMarkers = L.layerGroup().addTo(map);

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

        map._nuiMarkers.addLayer(marker);
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
