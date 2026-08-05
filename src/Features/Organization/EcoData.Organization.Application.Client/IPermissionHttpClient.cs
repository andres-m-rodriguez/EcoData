using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using OneOf;

namespace EcoData.Organization.Application.Client;

public interface IPermissionHttpClient
{
    Task<OneOf<UserPermissionsDto, RequestFailed>> GetMyPermissionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}
