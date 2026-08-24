using EcoData.Ui.Interop;

namespace FaunaFinder.Client.Services.Geolocation;

public enum GeoStatus
{
    Ok,
    Denied,
    Unavailable,
    Unsupported,
}

public sealed record GeoPosition(GeoStatus Status, double Latitude, double Longitude);

public static class BrowserGeolocation
{
    private const string ModulePath = "./js/fauna-geo.js";

    private static readonly GeoPosition Unavailable = new(GeoStatus.Unavailable, 0, 0);

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

    private sealed record RawPosition(string Status, double Latitude, double Longitude);
}
