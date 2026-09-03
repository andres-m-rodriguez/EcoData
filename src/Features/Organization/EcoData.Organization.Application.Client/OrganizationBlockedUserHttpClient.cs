using System.Net.Http.Json;
using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using OneOf;
using OneOf.Types;

namespace EcoData.Organization.Application.Client;

public sealed class OrganizationBlockedUserHttpClient(HttpClient httpClient)
    : IOrganizationBlockedUserHttpClient
{
    public IAsyncEnumerable<OrganizationBlockedUserDto> GetBlockedUsersAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        return httpClient.GetFromJsonAsAsyncEnumerable<OrganizationBlockedUserDto>(
            $"organization/organizations/{organizationId}/blocked-users",
            cancellationToken
        )!;
    }

    public async Task<OneOf<OrganizationBlockedUserDto, RequestFailed>> BlockUserAsync(
        Guid organizationId,
        Guid userId,
        string? reason,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var request = new BlockUserRequest(userId, reason);
            var response = await httpClient.PostAsJsonAsync(
                $"organization/organizations/{organizationId}/blocked-users",
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<OrganizationBlockedUserDto>(cancellationToken);
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

    public async Task<OneOf<Success, RequestFailed>> UnblockUserAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.DeleteAsync(
                $"organization/organizations/{organizationId}/blocked-users/{userId}",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            return new Success();
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
