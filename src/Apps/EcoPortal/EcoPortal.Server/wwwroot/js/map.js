window.locationPicker = {
    map: null,
    marker: null,
    dotNetRef: null,

    init: function (elementId, dotNetRef, initialLat, initialLng) {
        if (this.map) {
            this.map.remove();
        }

        this.dotNetRef = dotNetRef;

        // Use initial coordinates or default to Puerto Rico
        const lat = initialLat || 18.2208;
        const lng = initialLng || -66.5901;
        const zoom = initialLat ? 12 : 9;

        this.map = L.map(elementId).setView([lat, lng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(this.map);

        // Add initial marker if coordinates provided
        if (initialLat && initialLng) {
            this.marker = L.marker([initialLat, initialLng]).addTo(this.map);
        }

        // Invalidate size after a short delay to ensure container is fully rendered
        setTimeout(() => {
            this.map.invalidateSize();
        }, 100);

        // Handle map clicks
        this.map.on('click', (e) => {
            const { lat, lng } = e.latlng;

            // Update or create marker
            if (this.marker) {
                this.marker.setLatLng([lat, lng]);
            } else {
                this.marker = L.marker([lat, lng]).addTo(this.map);
            }

            // Notify Blazor
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnLocationSelected', lat, lng);
            }
        });
    },

    setLocation: function (lat, lng) {
        if (!this.map) return;

        if (this.marker) {
            this.marker.setLatLng([lat, lng]);
        } else {
            this.marker = L.marker([lat, lng]).addTo(this.map);
        }

        this.map.setView([lat, lng], 12);
    },

    dispose: function () {
        if (this.map) {
            this.map.remove();
            this.map = null;
            this.marker = null;
            this.dotNetRef = null;
        }
    }
};

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
