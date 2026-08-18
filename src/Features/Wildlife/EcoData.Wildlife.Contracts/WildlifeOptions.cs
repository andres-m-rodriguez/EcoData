namespace EcoData.Wildlife.Contracts;

public sealed class WildlifeOptions
{
    public const string SectionName = "Wildlife";

    /// <summary>
    /// Denominator for the catalogue's municipality-coverage stat.
    /// </summary>
    /// <remarks>
    /// The Wildlife module holds municipality ids as cross-module references only and has no
    /// municipality table of its own, so it cannot count them. Hosts that reference both
    /// modules override this from the live Locations count; the default is a fallback for
    /// hosts that do not.
    ///
    /// This deliberately spans every seeded jurisdiction rather than Puerto Rico alone. The
    /// catalogue covers the FWS Caribbean field office's area — Puerto Rico's 78 municipios
    /// and the 3 U.S. Virgin Islands — and the numerator already counts across both, so a
    /// Puerto Rico-only denominator reported coverage against the wrong whole.
    /// </remarks>
    public int TotalMunicipalities { get; set; } = 81;
}
