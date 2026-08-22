namespace EcoData.Common.Authorization;

public interface IAuthorization
{
    Task<bool> HasAsync(
        IOrganizationPermission permission,
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<bool> HasAsync(
        IGlobalPermission permission,
        CancellationToken cancellationToken = default
    );
}
