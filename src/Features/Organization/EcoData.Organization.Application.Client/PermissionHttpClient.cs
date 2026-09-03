using System.Net.Http.Json;
using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using OneOf;

namespace EcoData.Organization.Application.Client;

public sealed class PermissionHttpClient(HttpClient httpClient) : IPermissionHttpClient
{
    public async Task<OneOf<UserPermissionsDto, RequestFailed>> GetMyPermissionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"organization/organizations/{organizationId}/my-permissions",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<UserPermissionsDto>(cancellationToken);
            if (result is null)
                return new RequestFailed(
                    (int)response.StatusCode,
                    "The server returned an empty response."
                );

            return result;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
