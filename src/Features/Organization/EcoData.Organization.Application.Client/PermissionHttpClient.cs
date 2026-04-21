using System.Net.Http.Json;
using EcoData.Organization.Contracts.Dtos;

namespace EcoData.Organization.Application.Client;

public sealed class PermissionHttpClient(HttpClient httpClient) : IPermissionHttpClient
{
    public async Task<UserPermissionsDto> GetMyPermissionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        var result = await httpClient.GetFromJsonAsync<UserPermissionsDto>(
            $"organization/organizations/{organizationId}/my-permissions",
            cancellationToken
        );

        return result!;
    }
}
