// FaunaFinder field notebook storage.
//
// FaunaFinder has no accounts, so the reader's own state — what they pinned and
// what they have looked at — lives in localStorage under two keys owned by the
// FieldNotebook service on the .NET side:
//   faunafinder-notebook-saved    pinned entries
//   faunafinder-notebook-recent   visited entries
//
// This module knows nothing about the shape of what it stores: it moves a JSON
// array in and a JSON string out, and treats anything it cannot make sense of
// as "nothing stored yet". Every localStorage touch is wrapped, because the API
// exists but throws outright in some private-browsing modes — reading the
// property is not enough to know whether it works.

// The stored array, or an empty one if there is nothing there, storage is
// unavailable, or the blob is no longer parseable JSON.
export function read(key) {
    let raw;

    try {
        raw = window.localStorage.getItem(key);
    } catch {
        // storage unavailable — the notebook is simply empty this session
        return [];
    }

    if (!raw) {
        return [];
    }

    try {
        const parsed = JSON.parse(raw);
        return Array.isArray(parsed) ? parsed : [];
    } catch {
        // hand-edited or truncated blob — start over rather than fail the page
        return [];
    }
}

// Stores the value under the key. .NET hands this a JSON string it serialized
// itself; anything else is stringified here so the module stays usable on its
// own. Failures are silent: the write just doesn't survive the reload.
export function write(key, value) {
    const raw = typeof value === 'string' ? value : JSON.stringify(value);

    try {
        window.localStorage.setItem(key, raw);
    } catch {
        // storage unavailable or quota exceeded — session-only notebook
    }
}
