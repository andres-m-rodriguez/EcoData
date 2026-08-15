namespace EcoData.Spa.Map;

/// <summary>
/// A circular overlay on the map (e.g. a generalized species occurrence area).
/// </summary>
public sealed record MapCircle(
    MapCoordinate Center,
    double RadiusMeters,
    string FillColor,
    string StrokeColor,
    string? PopupHtml
);
