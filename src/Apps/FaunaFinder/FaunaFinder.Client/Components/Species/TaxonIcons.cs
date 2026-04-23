using System.Collections.Frozen;

namespace FaunaFinder.Client.Components.Species;

/// <summary>
/// Maps taxon codes to Font Awesome 6 free class names + translation keys.
/// Material Icons lacks proper taxonomic iconography (no bird/frog/dragon),
/// so we fall through to FA for the taxon badges. Labels are indirected
/// through ILocalizer keys (<see cref="GetLabelKey"/>) so call-sites can
/// translate them at render time.
/// </summary>
public static class TaxonIcons
{
    private static readonly FrozenDictionary<string, string> Map =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bird"] = "fa-solid fa-crow",
            ["plant"] = "fa-solid fa-leaf",
            ["reptile"] = "fa-solid fa-dragon",
            ["amphib"] = "fa-solid fa-frog",
            ["fish"] = "fa-solid fa-fish",
            ["mammal"] = "fa-solid fa-paw",
            ["invert"] = "fa-solid fa-bug",
            ["fungi"] = "fa-solid fa-seedling",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, string> LabelKeys =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bird"] = "Species_Taxa_Bird",
            ["plant"] = "Species_Taxa_Plant",
            ["reptile"] = "Species_Taxa_Reptile",
            ["amphib"] = "Species_Taxa_Amphib",
            ["fish"] = "Species_Taxa_Fish",
            ["mammal"] = "Species_Taxa_Mammal",
            ["invert"] = "Species_Taxa_Invert",
            ["fungi"] = "Species_Taxa_Fungi",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> OrderedCodes { get; } =
        ["bird", "plant", "reptile", "amphib", "fish", "mammal", "invert", "fungi"];

    public static string GetIcon(string? code) =>
        code is not null && Map.TryGetValue(code, out var icon)
            ? icon
            : "fa-solid fa-paw";

    /// <summary>
    /// Returns the translation key for the taxon label (e.g. <c>"Species_Taxa_Bird"</c>).
    /// Resolve via <c>ILocalizer</c> at the call site.
    /// </summary>
    public static string GetLabelKey(string? code) =>
        code is not null && LabelKeys.TryGetValue(code, out var key)
            ? key
            : "Species_Taxa_Bird";
}
