namespace EcoData.Spa.Map;

/// <summary>
/// Result of a browser geolocation request. <see cref="Error"/> is one of
/// "unsupported", "denied", "unavailable", or "timeout" when <see cref="Success"/> is false.
/// </summary>
public sealed record MapGeolocationResult(
    bool Success,
    double Latitude,
    double Longitude,
    string? Error
);
