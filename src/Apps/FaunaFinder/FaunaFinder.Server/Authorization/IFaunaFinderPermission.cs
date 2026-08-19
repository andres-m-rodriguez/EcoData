namespace FaunaFinder.Server.Authorization;

/// <summary>
/// What a signed-in user may do in FaunaFinder. One method per permission, so no caller
/// types a permission key — or needs to know an organization is involved at all.
/// </summary>
/// <remarks>
/// Contributor rights currently come from membership of the organization that runs
/// FaunaFinder. If a second institution ever contributes, this interface is where that
/// changes, and no call site moves.
/// </remarks>
public interface IFaunaFinderPermission
{
    Task<bool> CanSubmitOccurrenceAsync(CancellationToken cancellationToken = default);

    Task<bool> CanVerifyOccurrenceAsync(CancellationToken cancellationToken = default);
}
