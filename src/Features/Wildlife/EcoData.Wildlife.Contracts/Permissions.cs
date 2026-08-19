namespace EcoData.Wildlife.Contracts;

/// <summary>
/// Permissions the Wildlife module defines. Namespaced by feature, never by app — which
/// application grants them is a deployment decision, the permission is a domain fact.
/// </summary>
public static class WildlifePermissions
{
    public const string ReadSpecies = "wildlife:species:read";
    public const string SubmitOccurrence = "wildlife:occurrence:submit";
    public const string VerifyOccurrence = "wildlife:occurrence:verify";
}
