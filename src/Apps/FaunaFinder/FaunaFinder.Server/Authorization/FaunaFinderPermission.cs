using EcoData.Common.Authorization;
using EcoData.Wildlife.Contracts;

namespace FaunaFinder.Server.Authorization;

public sealed class FaunaFinderPermission(
    IAuthorization authorization,
    FaunaFinderOrganization organization
) : IFaunaFinderPermission
{
    public Task<bool> CanSubmitOccurrenceAsync(CancellationToken cancellationToken = default) =>
        HasAsync(WildlifePermissions.SubmitOccurrence, cancellationToken);

    public Task<bool> CanVerifyOccurrenceAsync(CancellationToken cancellationToken = default) =>
        HasAsync(WildlifePermissions.VerifyOccurrence, cancellationToken);

    private Task<bool> HasAsync(string permission, CancellationToken cancellationToken)
    {
        // Unresolved organization means the deployment has no contributor org configured;
        // deny rather than ask, since there is no scope to ask about.
        if (organization.Id is not { } organizationId)
            return Task.FromResult(false);

        return authorization.HasPermissionAsync(
            PermissionScope.Organization(organizationId),
            permission,
            cancellationToken
        );
    }
}
