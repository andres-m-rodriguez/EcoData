namespace EcoData.Wildlife.Contracts;

public sealed class WildlifeOptions
{
    public const string SectionName = "Wildlife";

    // Fallback coverage denominator when the host doesn't override from Locations:
    // Puerto Rico's 78 municipios plus the 3 U.S. Virgin Islands.
    public int TotalMunicipalities { get; set; } = 81;
}
