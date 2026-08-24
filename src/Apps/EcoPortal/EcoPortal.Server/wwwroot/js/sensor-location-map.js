// Read-only Leaflet map marking a sensor's position; one instance per element.
window.sensorLocationMap = {
    instances: new Map(),

    init: function (elementId, latitude, longitude) {
        if (this.instances.has(elementId)) {
            this.dispose(elementId);
        }

        const map = L.map(elementId, {
            zoomControl: false,
            dragging: false,
            scrollWheelZoom: false,
            doubleClickZoom: false,
            boxZoom: false,
            keyboard: false,
            touchZoom: false
        }).setView([latitude, longitude], 13);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(map);

        L.marker([latitude, longitude]).addTo(map);

        // Leaflet measures its host on creation, which can predate layout.
        setTimeout(() => map.invalidateSize(), 100);

        this.instances.set(elementId, map);
    },

    dispose: function (elementId) {
        const map = this.instances.get(elementId);
        if (!map) return;
        map.remove();
        this.instances.delete(elementId);
    }
};
