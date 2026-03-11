using EcoData.AquaTrack.Contracts.Dtos;

namespace EcoData.AquaTrack.Application.Client;

public interface IPermissionHttpClient
{
    Task<UserPermissionsDto> GetMyPermissionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}
