// Browser geolocation, shaped for Blazor interop.
//
// getPosition never rejects. A rejected promise surfaces in .NET as a
// JSException, which would force every caller into a try/catch just to tell
// "the user said no" apart from "the browser has no geolocation" — so the
// outcome is carried in the payload instead and the promise always resolves.

const DENIED = 1;

export function getPosition(timeoutMs) {
    return new Promise((resolve) => {
        if (!navigator.geolocation) {
            resolve({ status: 'unsupported', latitude: 0, longitude: 0 });
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => resolve({
                status: 'ok',
                latitude: position.coords.latitude,
                longitude: position.coords.longitude,
            }),
            (error) => resolve({
                status: error.code === DENIED ? 'denied' : 'unavailable',
                latitude: 0,
                longitude: 0,
            }),
            {
                timeout: timeoutMs > 0 ? timeoutMs : 10000,
                // A minute-old fix is fine for "what is near me" and spares
                // the device a fresh GPS lock on every chip press.
                maximumAge: 60000,
            });
    });
}
