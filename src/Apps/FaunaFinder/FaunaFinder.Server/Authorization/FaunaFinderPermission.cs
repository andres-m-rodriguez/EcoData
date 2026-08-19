using EcoData.Common.Authorization;
using EcoData.Wildlife.Contracts;

namespace FaunaFinder.Server.Authorization;

public sealed class FaunaFinderPermission(
    IAuthorization authorization,
    FaunaFinderOrganizationAccessor accessor
) : IFaunaFinderPermission
{
    public Task<bool> CanSubmitOccurrenceAsync(CancellationToken cancellationToken = default) =>
        HasAsync(WildlifePermissions.SubmitOccurrence, cancellationToken);

    public Task<bool> CanVerifyOccurrenceAsync(CancellationToken cancellationToken = default) =>
        HasAsync(WildlifePermissions.VerifyOccurrence, cancellationToken);

    private Task<bool> HasAsync(string permission, CancellationToken cancellationToken)
    {
        // No configured organization means there is no scope to ask about, so deny rather
        // than ask. The single check here is why the accessor holds one nullable reference
        // instead of a nullable field per property.
        if (accessor.Organization is not { } organization)
            return Task.FromResult(false);

        return authorization.HasPermissionAsync(
            PermissionScope.Organization(organization.Id),
            permission,
            cancellationToken
        );
    }
}
