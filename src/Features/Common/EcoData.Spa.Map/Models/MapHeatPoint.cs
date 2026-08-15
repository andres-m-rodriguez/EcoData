namespace EcoData.Spa.Map;

/// <summary>
/// A weighted point for the heatmap layer. <see cref="IsFauna"/> feeds the
/// fauna/flora heatmap filter.
/// </summary>
public sealed record MapHeatPoint(
    double Latitude,
    double Longitude,
    double Intensity,
    bool IsFauna
);

/// <summary>
/// Which heat points are rendered: all of them, fauna only, or flora only.
/// </summary>
public enum MapHeatmapFilter
{
    All,
    Fauna,
    Flora,
}
