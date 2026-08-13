// FaunaFinder light/dark switch.
//
// The token sheet keys off a data-theme attribute on <html>, in three states:
//   data-theme="dark"   explicit dark
//   data-theme="light"  explicit light
//   no attribute        follow the OS via prefers-color-scheme
//
// The inline boot script in App.razor stamps the stored choice before first
// paint; this module is only what the layout calls once Blazor is running.

const STORAGE_KEY = 'faunafinder-theme';

function prefersDark() {
    return typeof window.matchMedia === 'function'
        && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

// The theme actually on screen right now: the stamped choice if there is one,
// otherwise whatever the OS is asking for.
export function getTheme() {
    const stamped = document.documentElement.getAttribute('data-theme');
    if (stamped === 'dark' || stamped === 'light') {
        return stamped;
    }

    return prefersDark() ? 'dark' : 'light';
}

// Stamps the attribute and remembers the choice. Private-mode browsers throw
// on localStorage writes; the stamp still lands, the choice just won't survive
// a reload.
export function setTheme(theme) {
    const next = theme === 'dark' ? 'dark' : 'light';
    document.documentElement.setAttribute('data-theme', next);

    try {
        window.localStorage.setItem(STORAGE_KEY, next);
    } catch {
        // storage unavailable — session-only theme
    }

    return next;
}
