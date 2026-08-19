namespace FaunaFinder.Server.Authorization;

public sealed class FaunaFinderOptions
{
    public const string SectionName = "FaunaFinder";

    /// <summary>
    /// The organization whose members may contribute to FaunaFinder.
    /// </summary>
    /// <remarks>
    /// A slug rather than an id: organizations are created at runtime, so the guid differs
    /// per environment and cannot be committed as configuration. The slug is stable, and
    /// <c>organizations.slug</c> carries a unique index.
    /// </remarks>
    public string OrganizationSlug { get; set; } = string.Empty;
}
