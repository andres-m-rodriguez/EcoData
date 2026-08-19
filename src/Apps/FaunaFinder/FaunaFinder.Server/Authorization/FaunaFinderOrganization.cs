namespace FaunaFinder.Server.Authorization;

/// <summary>
/// The resolved id of the organization FaunaFinder contributes to, filled in once at
/// startup by <see cref="FaunaFinderOrganizationResolver"/>.
/// </summary>
/// <remarks>
/// <see cref="Id"/> stays null when the configured slug matches no organization. That is
/// not fatal — FaunaFinder is a public catalogue first, so it serves anonymous traffic
/// normally and every contributor permission simply answers false.
/// </remarks>
public sealed class FaunaFinderOrganization
{
    public Guid? Id { get; internal set; }
}
