using System.Collections.Frozen;

namespace FaunaFinder.Client.Components.Species;

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

    public static string GetLabelKey(string? code) =>
        code is not null && LabelKeys.TryGetValue(code, out var key)
            ? key
            : "Species_Taxa_Bird";
}
