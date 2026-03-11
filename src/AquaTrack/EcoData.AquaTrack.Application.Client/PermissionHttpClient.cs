using System.Net.Http.Json;
using EcoData.AquaTrack.Contracts.Dtos;

namespace EcoData.AquaTrack.Application.Client;

public sealed class PermissionHttpClient(HttpClient httpClient) : IPermissionHttpClient
{
    public async Task<UserPermissionsDto> GetMyPermissionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        var result = await httpClient.GetFromJsonAsync<UserPermissionsDto>(
            $"api/organizations/{organizationId}/my-permissions",
            cancellationToken
        );

        return result!;
    }
}
