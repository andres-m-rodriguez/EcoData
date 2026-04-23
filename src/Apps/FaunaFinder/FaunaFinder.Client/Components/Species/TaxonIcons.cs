using System.Collections.Frozen;
using MudBlazor;

namespace FaunaFinder.Client.Components.Species;

public static class TaxonIcons
{
    private static readonly FrozenDictionary<string, string> Map =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bird"] = Icons.Material.Filled.FlightTakeoff,
            ["plant"] = Icons.Material.Filled.LocalFlorist,
            ["reptile"] = Icons.Material.Filled.Pets,
            ["amphib"] = Icons.Material.Filled.WaterDrop,
            ["fish"] = Icons.Material.Filled.SetMeal,
            ["mammal"] = Icons.Material.Filled.Pets,
            ["invert"] = Icons.Material.Filled.BugReport,
            ["fungi"] = Icons.Material.Filled.Spa,
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
            : Icons.Material.Filled.Pets;

    public static string GetLabel(string? code) =>
        code is not null && Labels.TryGetValue(code, out var label)
            ? label
            : code ?? "—";
}
