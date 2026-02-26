window.sensorMap = {
    map: null,
    markersLayer: null,

    init: function (elementId) {
        if (this.map) {
            this.map.remove();
        }

        // Center on Puerto Rico
        this.map = L.map(elementId).setView([18.2208, -66.5901], 9);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(this.map);

        this.markersLayer = L.featureGroup().addTo(this.map);
    },

    addSensors: function (sensors) {
        if (!this.markersLayer) return;

        this.markersLayer.clearLayers();

        sensors.forEach(sensor => {
            const marker = L.circleMarker([sensor.latitude, sensor.longitude], {
                radius: 6,
                fillColor: '#1976d2',
                color: '#fff',
                weight: 2,
                opacity: 1,
                fillOpacity: 0.8
            });

            const popupContent = `
                <div style="min-width: 150px;">
                    <strong>${sensor.name}</strong><br/>
                    <small>ID: ${sensor.externalId}</small><br/>
                    ${sensor.municipality ? `<small>Municipality: ${sensor.municipality}</small><br/>` : ''}
                    <small>Status: ${sensor.isActive ? 'Active' : 'Inactive'}</small>
                </div>
            `;

            marker.bindPopup(popupContent);
            this.markersLayer.addLayer(marker);
        });

        // Fit bounds if there are sensors
        if (sensors.length > 0) {
            const bounds = this.markersLayer.getBounds();
            if (bounds.isValid()) {
                this.map.fitBounds(bounds, { padding: [50, 50] });
            }
        }
    },

    dispose: function () {
        if (this.map) {
            this.map.remove();
            this.map = null;
            this.markersLayer = null;
        }
    }
};
