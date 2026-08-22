using EcoData.Ui.Interop;

namespace FaunaFinder.Client.Services.Geolocation;

/// <summary>How a location request ended.</summary>
public enum GeoStatus
{
    Ok,
    Denied,
    Unavailable,
    Unsupported,
}

/// <summary>
/// A resolved location request. <see cref="Latitude"/> and
/// <see cref="Longitude"/> are only meaningful when <see cref="Status"/> is
/// <see cref="GeoStatus.Ok"/>.
/// </summary>
public sealed record GeoPosition(GeoStatus Status, double Latitude, double Longitude);

/// <summary>
/// Reads the browser's location.
///
/// <para>The map has its own geolocation through <c>IMapController</c>, but
/// that is welded to the <c>SpaMap</c> component — anything outside a map
/// would have to render a hidden one to borrow it. This is the same
/// capability without the component.</para>
/// </summary>
public static class BrowserGeolocation
{
    private const string ModulePath = "./js/fauna-geo.js";

    private static readonly GeoPosition Unavailable = new(GeoStatus.Unavailable, 0, 0);

    /// <summary>
    /// Asks the browser where it is. Never throws: an interop failure is
    /// reported as <see cref="GeoStatus.Unavailable"/>, the same as the
    /// browser failing to get a fix.
    /// </summary>
    public static async Task<GeoPosition> GetPositionAsync(
        IJavascriptSafeInterop js,
        int timeoutMs = 10000,
        CancellationToken ct = default
    )
    {
        var imported = await js.ImportAsync(ModulePath, ct);
        if (!imported.TryPickT0(out var module, out _))
        {
            return Unavailable;
        }

        var position = await js.InvokeAsync<RawPosition>(module, "getPosition", ct, timeoutMs);
        await js.DisposeAsync(module);

        if (!position.TryPickT0(out var raw, out _))
        {
            return Unavailable;
        }

        var status = raw.Status switch
        {
            "ok" => GeoStatus.Ok,
            "denied" => GeoStatus.Denied,
            "unsupported" => GeoStatus.Unsupported,
            _ => GeoStatus.Unavailable,
        };

        return new GeoPosition(status, raw.Latitude, raw.Longitude);
    }

    // Mirrors the module's payload; the JS side never rejects, so status is
    // always present.
    private sealed record RawPosition(string Status, double Latitude, double Longitude);
}
