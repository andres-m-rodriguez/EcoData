// Leaflet map interop for NuiMap component

const maps = new Map();

export function initialize(element, lat, lng, zoom) {
    // Create the map instance
    const map = L.map(element).setView([lat, lng], zoom);

    // Add OpenStreetMap tile layer
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    // Store markers layer group
    map._nuiMarkers = L.layerGroup().addTo(map);

    // Generate unique ID
    const id = crypto.randomUUID();
    maps.set(id, map);

    return { id, map };
}

export function setView(mapRef, lat, lng, zoom) {
    const map = mapRef.map || maps.get(mapRef.id);
    if (map) {
        map.setView([lat, lng], zoom);
    }
}

export function setMarkers(mapRef, markers) {
    const map = mapRef.map || maps.get(mapRef.id);
    if (!map) return;

    // Clear existing markers
    map._nuiMarkers.clearLayers();

    // Add new markers
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

export function fitToMarkers(mapRef) {
    const map = mapRef.map || maps.get(mapRef.id);
    if (!map) return;

    const layers = map._nuiMarkers.getLayers();
    if (layers.length > 0) {
        const group = L.featureGroup(layers);
        map.fitBounds(group.getBounds(), { padding: [20, 20] });
    }
}

export function fitToBounds(mapRef, southWestLat, southWestLng, northEastLat, northEastLng) {
    const map = mapRef.map || maps.get(mapRef.id);
    if (!map) return;

    const bounds = L.latLngBounds(
        [southWestLat, southWestLng],
        [northEastLat, northEastLng]
    );
    map.fitBounds(bounds);
}

export function dispose(mapRef) {
    const id = mapRef.id;
    const map = mapRef.map || maps.get(id);

    if (map) {
        map.remove();
        maps.delete(id);
    }
}
