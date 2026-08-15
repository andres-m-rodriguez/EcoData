// Leaflet map interop for SpaMap component

const OSM_ATTRIBUTION =
    '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';

// One basemap per ground. The host decides which by stamping data-theme on
// <html> — the same signal the token sheets key off — so the tiles flip with
// the rest of the shell instead of staying paper-white under a dark UI.
const BASEMAPS = {
    light: {
        url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
        options: { attribution: OSM_ATTRIBUTION }
    },
    dark: {
        url: 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png',
        options: {
            attribution:
                OSM_ATTRIBUTION +
                ' &copy; <a href="https://carto.com/attributions">CARTO</a>',
            subdomains: 'abcd',
            maxZoom: 20
        }
    }
};

// Three states, matching the token sheets: an explicit stamp wins, and with
// nothing stamped the OS decides.
function resolveTheme() {
    const stamped = document.documentElement.getAttribute('data-theme');
    if (stamped === 'dark' || stamped === 'light') {
        return stamped;
    }

    return typeof window.matchMedia === 'function'
        && window.matchMedia('(prefers-color-scheme: dark)').matches
        ? 'dark'
        : 'light';
}

function applyBasemap(map, theme) {
    if (map._spaTheme === theme) return;

    if (map._spaTiles) {
        map.removeLayer(map._spaTiles);
    }

    const basemap = BASEMAPS[theme] ?? BASEMAPS.light;
    map._spaTiles = L.tileLayer(basemap.url, basemap.options).addTo(map);
    // Added last, so it would otherwise sit over the markers and polygons
    // already on the map when the theme flips mid-session.
    map._spaTiles.bringToBack();
    map._spaTheme = theme;
}

// A map can outlive any one theme, so it watches both sources rather than
// reading once at init.
function watchTheme(map) {
    const onThemeChanged = () => applyBasemap(map, resolveTheme());

    const observer = new MutationObserver(onThemeChanged);
    observer.observe(document.documentElement, {
        attributes: true,
        attributeFilter: ['data-theme']
    });

    const media = typeof window.matchMedia === 'function'
        ? window.matchMedia('(prefers-color-scheme: dark)')
        : null;
    media?.addEventListener('change', onThemeChanged);

    map._spaThemeWatch = { observer, media, onThemeChanged };
}

export function initialize(element, lat, lng, zoom, dotNetRef) {
    const map = L.map(element).setView([lat, lng], zoom);

    map._spaTiles = null;
    map._spaTheme = null;
    applyBasemap(map, resolveTheme());
    watchTheme(map);

    map._spaMarkers = L.layerGroup().addTo(map);
    map._spaGeoJson = L.layerGroup().addTo(map);
    map._spaCircles = L.layerGroup().addTo(map);
    map._dotNetRef = dotNetRef;
    map._spaSearchRadius = null;
    map._spaUserLocation = null;
    map._spaHeat = { layer: null, points: [], filter: 'all' };
    map._spaDraw = { active: false, points: [], markers: [], polygon: null, previewLine: null, handlers: null };
    map._spaGeoJsonGeneration = 0;

    // Map click handler (suppressed while drawing a polygon)
    map.on('click', (e) => {
        if (map._spaDraw.active) return;
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

    map._spaMarkers.clearLayers();

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

        map._spaMarkers.addLayer(marker);
    });
}

export function setGeoJson(map, layers) {
    if (!map) return;

    map._spaGeoJson.clearLayers();
    // Invalidates URL fetches still in flight from a previous setGeoJson call,
    // so they don't add layers on top of the cleared state.
    const generation = ++map._spaGeoJsonGeneration;

    layers.forEach(layer => {
        if (layer.data) {
            try {
                addGeoJsonLayer(map, layer, JSON.parse(layer.data));
            } catch (e) {
                console.error('Failed to parse GeoJSON for layer:', layer.id, e);
            }
        } else if (layer.url) {
            loadGeoJsonFromUrl(map, layer, generation);
        }
    });
}

function loadGeoJsonFromUrl(map, layer, generation) {
    const notify = (success) => {
        if (map._dotNetRef && map._spaGeoJsonGeneration === generation) {
            map._dotNetRef.invokeMethodAsync('OnGeoJsonLoadedFromJs', layer.id, success);
        }
    };

    if (layer.cacheKey) {
        const versionKey = layer.cacheKey + ':version';
        try {
            const cached = localStorage.getItem(layer.cacheKey);
            if (cached && localStorage.getItem(versionKey) === layer.cacheVersion) {
                addGeoJsonLayer(map, layer, JSON.parse(cached));
                notify(true);
                return;
            }
        } catch (e) {
            console.warn('Failed to read cached GeoJSON, fetching from URL:', e);
        }
    }

    fetch(layer.url)
        .then(r => {
            if (!r.ok) throw new Error('GeoJSON request returned ' + r.status);
            return r.text();
        })
        .then(text => {
            const data = JSON.parse(text);
            if (map._spaGeoJsonGeneration !== generation) return;

            if (layer.cacheKey) {
                try {
                    localStorage.setItem(layer.cacheKey, text);
                    localStorage.setItem(layer.cacheKey + ':version', layer.cacheVersion);
                } catch (e) {
                    console.warn('Failed to cache GeoJSON to localStorage:', e);
                }
            }
            addGeoJsonLayer(map, layer, data);
            notify(true);
        })
        .catch(e => {
            console.error('Failed to load GeoJSON for layer:', layer.id, e);
            notify(false);
        });
}

function addGeoJsonLayer(map, layer, geoJsonData) {
    const geoJsonLayer = L.geoJSON(geoJsonData, {
        style: {
            fillColor: layer.fillColor,
            fillOpacity: layer.fillOpacity,
            color: layer.strokeColor,
            weight: layer.strokeWidth
        },
        onEachFeature: (feature, leafletLayer) => {
            // GeoJSON feature click handler. While drawing a polygon the
            // click must bubble to the map's draw handler instead, so the
            // feature neither swallows the vertex placement nor selects.
            leafletLayer.on('click', (e) => {
                if (map._spaDraw.active) return;
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
    map._spaGeoJson.addLayer(geoJsonLayer);
}

export function fitToMarkers(map) {
    if (!map) return;

    const layers = map._spaMarkers.getLayers();
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

// ===== Circles (species occurrence areas, generalized as circles) =====

export function setCircles(map, circles) {
    if (!map) return;

    map._spaCircles.clearLayers();

    circles.forEach(c => {
        const circle = L.circle([c.lat, c.lng], {
            radius: c.radiusMeters,
            fillColor: c.fillColor,
            color: c.strokeColor,
            weight: 2,
            fillOpacity: 0.35
        });

        if (c.popup) {
            circle.bindPopup(c.popup);
        }

        circle.on('click', () => {
            if (map._dotNetRef) {
                map._dotNetRef.invokeMethodAsync('OnCircleClickedFromJs', c.index);
            }
        });

        map._spaCircles.addLayer(circle);
    });
}

export function clearCircles(map) {
    if (!map) return;
    map._spaCircles.clearLayers();
}

export function focusCircle(map, index) {
    if (!map) return;

    const layers = map._spaCircles.getLayers();
    if (index < 0 || index >= layers.length) return;

    const circle = layers[index];
    map.flyToBounds(circle.getBounds(), { padding: [50, 50], maxZoom: 14, duration: 0.8 });
    setTimeout(() => circle.openPopup(), 800);
}

export function focusAllCircles(map) {
    if (!map) return;

    const layers = map._spaCircles.getLayers();
    if (layers.length === 0) return;

    const group = L.featureGroup(layers);
    map.flyToBounds(group.getBounds(), { padding: [50, 50], duration: 0.8 });
}

// ===== Search radius + user location =====

export function showSearchRadius(map, lat, lng, radiusMeters) {
    if (!map) return;

    clearSearchRadius(map);

    map._spaUserLocation = L.circleMarker([lat, lng], {
        radius: 8,
        fillColor: '#2563eb',
        color: '#ffffff',
        weight: 3,
        fillOpacity: 1
    }).addTo(map);

    map._spaSearchRadius = L.circle([lat, lng], {
        radius: radiusMeters,
        fillColor: '#3b82f6',
        color: '#2563eb',
        weight: 2,
        fillOpacity: 0.08,
        dashArray: '8, 6'
    }).addTo(map);

    map.flyToBounds(map._spaSearchRadius.getBounds(), { padding: [30, 30], duration: 0.8 });
}

export function clearSearchRadius(map) {
    if (!map) return;

    if (map._spaSearchRadius) {
        map.removeLayer(map._spaSearchRadius);
        map._spaSearchRadius = null;
    }
    if (map._spaUserLocation) {
        map.removeLayer(map._spaUserLocation);
        map._spaUserLocation = null;
    }
}

// ===== Geolocation =====

export function getCurrentPosition() {
    return new Promise((resolve) => {
        if (!('geolocation' in navigator)) {
            resolve({ success: false, latitude: 0, longitude: 0, error: 'unsupported' });
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => resolve({
                success: true,
                latitude: position.coords.latitude,
                longitude: position.coords.longitude,
                error: null
            }),
            (error) => resolve({
                success: false,
                latitude: 0,
                longitude: 0,
                error: error.code === error.PERMISSION_DENIED ? 'denied'
                    : error.code === error.POSITION_UNAVAILABLE ? 'unavailable'
                    : 'timeout'
            }),
            { enableHighAccuracy: true, timeout: 10000, maximumAge: 60000 }
        );
    });
}

// ===== Polygon draw mode =====

export function enablePolygonDraw(map) {
    if (!map || map._spaDraw.active) return;

    const draw = map._spaDraw;
    draw.active = true;
    clearDrawnPolygon(map);

    const container = map.getContainer();
    container.classList.add('spa-map-draw-mode');
    map.dragging.disable();
    map.doubleClickZoom.disable();

    const updatePolygon = () => {
        if (draw.polygon) {
            map.removeLayer(draw.polygon);
            draw.polygon = null;
        }
        if (draw.points.length < 2) return;

        draw.polygon = L.polygon(draw.points, {
            fillColor: '#8b5cf6',
            color: '#7c3aed',
            weight: 3,
            fillOpacity: 0.2,
            dashArray: '10, 5'
        }).addTo(map);
    };

    const finish = () => {
        if (draw.points.length < 3) return;

        if (draw.previewLine) {
            map.removeLayer(draw.previewLine);
            draw.previewLine = null;
        }
        draw.markers.forEach(m => map.removeLayer(m));
        draw.markers = [];

        if (draw.polygon) {
            draw.polygon.setStyle({ dashArray: null, fillOpacity: 0.15 });
        }

        const coordinates = draw.points.map(p => ({ latitude: p.lat, longitude: p.lng }));
        disablePolygonDraw(map);

        if (map._dotNetRef) {
            map._dotNetRef.invokeMethodAsync('OnPolygonDrawnFromJs', coordinates);
        }
    };

    const onClick = (e) => {
        if (!draw.active) return;
        const point = e.latlng;

        // Clicking near the first vertex closes the polygon.
        if (draw.points.length >= 3) {
            const first = draw.points[0];
            const distance = map.latLngToContainerPoint(point)
                .distanceTo(map.latLngToContainerPoint(first));
            if (distance < 15) {
                finish();
                return;
            }
        }

        draw.points.push(point);
        const marker = L.circleMarker(point, {
            radius: 6,
            fillColor: draw.points.length === 1 ? '#22c55e' : '#8b5cf6',
            color: '#fff',
            weight: 2,
            fillOpacity: 1
        }).addTo(map);
        draw.markers.push(marker);
        updatePolygon();
    };

    const onMouseMove = (e) => {
        if (!draw.active || draw.points.length === 0) return;

        const last = draw.points[draw.points.length - 1];
        if (draw.previewLine) {
            map.removeLayer(draw.previewLine);
        }
        draw.previewLine = L.polyline([last, e.latlng], {
            color: '#8b5cf6',
            weight: 2,
            dashArray: '5, 5',
            opacity: 0.7
        }).addTo(map);
    };

    const onDblClick = (e) => {
        if (!draw.active) return;
        L.DomEvent.stopPropagation(e.originalEvent);
        L.DomEvent.preventDefault(e.originalEvent);
        finish();
    };

    const onKeyDown = (e) => {
        if (!draw.active) return;

        if (e.key === 'Escape') {
            clearDrawnPolygon(map);
            disablePolygonDraw(map);
            if (map._dotNetRef) {
                map._dotNetRef.invokeMethodAsync('OnPolygonDrawCancelledFromJs');
            }
        } else if (e.key === 'Enter' && draw.points.length >= 3) {
            finish();
        } else if ((e.key === 'Backspace' || e.key === 'Delete') && draw.points.length > 0) {
            draw.points.pop();
            const lastMarker = draw.markers.pop();
            if (lastMarker) {
                map.removeLayer(lastMarker);
            }
            updatePolygon();
        }
    };

    draw.handlers = { onClick, onMouseMove, onDblClick, onKeyDown, finish };
    map.on('click', onClick);
    map.on('mousemove', onMouseMove);
    map.on('dblclick', onDblClick);
    document.addEventListener('keydown', onKeyDown);
}

export function finishPolygonDraw(map) {
    if (!map || !map._spaDraw.active || !map._spaDraw.handlers) return;
    map._spaDraw.handlers.finish();
}

export function cancelPolygonDraw(map) {
    if (!map) return;
    clearDrawnPolygon(map);
    disablePolygonDraw(map);
}

export function getDrawnPointCount(map) {
    return map ? map._spaDraw.points.length : 0;
}

function disablePolygonDraw(map) {
    const draw = map._spaDraw;
    draw.active = false;

    map.getContainer().classList.remove('spa-map-draw-mode');
    map.dragging.enable();
    map.doubleClickZoom.enable();

    if (draw.previewLine) {
        map.removeLayer(draw.previewLine);
        draw.previewLine = null;
    }

    if (draw.handlers) {
        map.off('click', draw.handlers.onClick);
        map.off('mousemove', draw.handlers.onMouseMove);
        map.off('dblclick', draw.handlers.onDblClick);
        document.removeEventListener('keydown', draw.handlers.onKeyDown);
        draw.handlers = null;
    }
}

export function clearDrawnPolygon(map) {
    if (!map) return;

    const draw = map._spaDraw;
    if (draw.polygon) {
        map.removeLayer(draw.polygon);
        draw.polygon = null;
    }
    if (draw.previewLine) {
        map.removeLayer(draw.previewLine);
        draw.previewLine = null;
    }
    draw.markers.forEach(m => map.removeLayer(m));
    draw.markers = [];
    draw.points = [];
}

// ===== Heatmap (requires the leaflet.heat plugin; no-op without it) =====

export function showHeatmap(map, points, filter) {
    if (!map) return;

    map._spaHeat.points = points;
    map._spaHeat.filter = filter || 'all';
    updateHeatLayer(map);
}

export function setHeatmapFilter(map, filter) {
    if (!map) return;

    map._spaHeat.filter = filter;
    updateHeatLayer(map);
}

export function hideHeatmap(map) {
    if (!map) return;

    if (map._spaHeat.layer) {
        map.removeLayer(map._spaHeat.layer);
        map._spaHeat.layer = null;
    }
    map._spaHeat.points = [];
}

function updateHeatLayer(map) {
    if (typeof L.heatLayer !== 'function') {
        console.warn('spamap: leaflet.heat plugin is not loaded; heatmap is unavailable.');
        return;
    }

    if (map._spaHeat.layer) {
        map.removeLayer(map._spaHeat.layer);
        map._spaHeat.layer = null;
    }

    let filtered = map._spaHeat.points;
    if (map._spaHeat.filter === 'fauna') {
        filtered = filtered.filter(p => p.isFauna);
    } else if (map._spaHeat.filter === 'flora') {
        filtered = filtered.filter(p => !p.isFauna);
    }

    if (filtered.length === 0) return;

    const heatData = filtered.map(p => [p.latitude, p.longitude, p.intensity]);
    map._spaHeat.layer = L.heatLayer(heatData, {
        radius: 25,
        blur: 15,
        maxZoom: 17,
        max: 1.0,
        gradient: {
            0.0: '#3b82f6',
            0.25: '#22c55e',
            0.5: '#eab308',
            0.75: '#f97316',
            1.0: '#ef4444'
        }
    }).addTo(map);
}

export function dispose(map) {
    if (map) {
        if (map._spaDraw && map._spaDraw.handlers) {
            document.removeEventListener('keydown', map._spaDraw.handlers.onKeyDown);
        }
        // Both theme listeners are attached to document/window, so they outlive
        // the map element unless they come off explicitly.
        if (map._spaThemeWatch) {
            map._spaThemeWatch.observer.disconnect();
            map._spaThemeWatch.media?.removeEventListener(
                'change',
                map._spaThemeWatch.onThemeChanged);
            map._spaThemeWatch = null;
        }
        map.remove();
    }
}
