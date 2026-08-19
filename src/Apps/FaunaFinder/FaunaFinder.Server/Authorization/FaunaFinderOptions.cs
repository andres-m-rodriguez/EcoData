namespace FaunaFinder.Server.Authorization;

public sealed class FaunaFinderOptions
{
    public const string SectionName = "FaunaFinder";
    public string OrganizationSlug { get; set; } = string.Empty;
}
