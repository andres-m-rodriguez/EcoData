// Leaflet Map Service - manages multiple map instances by element ID
window.leafletMapService = {
    instances: new Map(),

    create: function (elementId, dotNetRef, initialLat, initialLng, initialZoom, showMarker) {
        // Dispose existing instance if any
        if (this.instances.has(elementId)) {
            this.dispose(elementId);
        }

        // Default to Puerto Rico if no coordinates
        const lat = initialLat || 18.2208;
        const lng = initialLng || -66.5901;
        const zoom = initialLat ? 12 : (initialZoom || 9);

        const map = L.map(elementId).setView([lat, lng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(map);

        const instance = {
            map: map,
            marker: null,
            dotNetRef: dotNetRef
        };

        // Add initial marker if requested
        if (showMarker && initialLat && initialLng) {
            instance.marker = L.marker([lat, lng]).addTo(map);
        }

        // Handle map clicks
        map.on('click', (e) => {
            const { lat, lng } = e.latlng;

            // Update or create marker
            if (instance.marker) {
                instance.marker.setLatLng([lat, lng]);
            } else {
                instance.marker = L.marker([lat, lng]).addTo(map);
            }

            // Notify Blazor
            if (instance.dotNetRef) {
                instance.dotNetRef.invokeMethodAsync('HandleMapClick', lat, lng);
            }
        });

        // Invalidate size after rendering
        setTimeout(() => map.invalidateSize(), 100);

        this.instances.set(elementId, instance);
    },

    setView: function (elementId, lat, lng, zoom) {
        const instance = this.instances.get(elementId);
        if (!instance) return;

        instance.map.setView([lat, lng], zoom || instance.map.getZoom());
    },

    setMarker: function (elementId, lat, lng) {
        const instance = this.instances.get(elementId);
        if (!instance) return;

        if (instance.marker) {
            instance.marker.setLatLng([lat, lng]);
        } else {
            instance.marker = L.marker([lat, lng]).addTo(instance.map);
        }

        instance.map.setView([lat, lng], 12);
    },

    clearMarker: function (elementId) {
        const instance = this.instances.get(elementId);
        if (!instance || !instance.marker) return;

        instance.map.removeLayer(instance.marker);
        instance.marker = null;
    },

    invalidateSize: function (elementId) {
        const instance = this.instances.get(elementId);
        if (!instance) return;

        instance.map.invalidateSize();
    },

    dispose: function (elementId) {
        const instance = this.instances.get(elementId);
        if (!instance) return;

        instance.map.remove();
        this.instances.delete(elementId);
    }
};

// Sensor Map - for displaying multiple sensors (used by SensorMapPage)
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
                    ${sensor.dataSourceName ? `<small>Source: ${sensor.dataSourceName}</small><br/>` : ''}
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
