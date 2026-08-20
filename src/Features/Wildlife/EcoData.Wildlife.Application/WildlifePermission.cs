using EcoData.Common.Authorization;

namespace EcoData.Wildlife.Application;

// One implementation for every host: IAuthorization resolves through whichever
// IPermissionSource that host registered, so client and server differ in wiring only.
public sealed class WildlifePermission(IAuthorization authorization) : IWildlifePermission
{
    public Task<bool> CanReadSpeciesAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    ) =>
        authorization.HasOrgPermissionAsync(
            organizationId,
            WildlifePermissions.ReadSpecies,
            cancellationToken
        );

    public Task<bool> CanSubmitOccurrenceAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    ) =>
        authorization.HasOrgPermissionAsync(
            organizationId,
            WildlifePermissions.SubmitOccurrence,
            cancellationToken
        );

    public Task<bool> CanVerifyOccurrenceAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    ) =>
        authorization.HasOrgPermissionAsync(
            organizationId,
            WildlifePermissions.VerifyOccurrence,
            cancellationToken
        );
}
