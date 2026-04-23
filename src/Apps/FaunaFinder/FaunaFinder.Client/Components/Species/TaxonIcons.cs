using System.Collections.Frozen;

namespace FaunaFinder.Client.Components.Species;

/// <summary>
/// Maps taxon codes to Font Awesome 6 free class names, loaded via CDN in App.razor.
/// Material Icons lacks proper taxonomic iconography (no bird/frog/dragon), so we fall
/// through to FA for the taxon badges specifically.
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

    private static readonly FrozenDictionary<string, string> Labels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bird"] = "Bird",
            ["plant"] = "Plant",
            ["reptile"] = "Reptile",
            ["amphib"] = "Amphibian",
            ["fish"] = "Fish",
            ["mammal"] = "Mammal",
            ["invert"] = "Invertebrate",
            ["fungi"] = "Fungus",
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> OrderedCodes { get; } =
        ["bird", "plant", "reptile", "amphib", "fish", "mammal", "invert", "fungi"];

    public static string GetIcon(string? code) =>
        code is not null && Map.TryGetValue(code, out var icon)
            ? icon
            : "fa-solid fa-paw";

    public static string GetLabel(string? code) =>
        code is not null && Labels.TryGetValue(code, out var label)
            ? label
            : code ?? "—";
}
