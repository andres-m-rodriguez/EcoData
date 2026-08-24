// Browser geolocation as a promise; never rejects, failures come back in the result.
window.epGeolocation = {
    getCurrentPosition: function () {
        return new Promise((resolve) => {
            if (!navigator.geolocation) {
                resolve({ success: false, error: "Geolocation is not supported by this browser" });
                return;
            }

            navigator.geolocation.getCurrentPosition(
                (position) => resolve({
                    success: true,
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                }),
                (error) => resolve({ success: false, error: error.message }),
                { enableHighAccuracy: true, timeout: 10000 }
            );
        });
    }
};
