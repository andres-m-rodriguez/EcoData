using EcoData.Organization.Contracts.Dtos;

namespace EcoData.Organization.Application.Client;

public interface IPermissionHttpClient
{
    Task<UserPermissionsDto> GetMyPermissionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}
