namespace FaunaFinder.Server.Authorization;

/// <summary>
/// Holds the resolved <see cref="FaunaFinderOrganization"/>, or nothing when the configured
/// slug matches no organization.
/// </summary>
/// <remarks>
/// One nullable reference rather than a nullable field per property: callers check once,
/// then work with non-null values. Absence is not fatal — FaunaFinder is a public catalogue
/// first, so it serves anonymous traffic normally and every contributor permission answers
/// false.
/// </remarks>
public sealed class FaunaFinderOrganizationAccessor
{
    public FaunaFinderOrganization? Organization { get; internal set; }
}
