using System.Net;
using System.Net.Http.Json;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Errors;
using OneOf;

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
            $"api/organizations/{organizationId}/blocked-users",
            cancellationToken
        )!;
    }

    public async Task<OneOf<OrganizationBlockedUserDto, ConflictError, ApiError>> BlockUserAsync(
        Guid organizationId,
        Guid userId,
        string? reason,
        CancellationToken cancellationToken = default
    )
    {
        var request = new BlockUserRequest(userId, reason);
        var response = await httpClient.PostAsJsonAsync(
            $"api/organizations/{organizationId}/blocked-users",
            request,
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ConflictError(message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ApiError(
                (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken)
            );
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationBlockedUserDto>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<Success, NotFoundError, ApiError>> UnblockUserAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.DeleteAsync(
            $"api/organizations/{organizationId}/blocked-users/{userId}",
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundError();
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ApiError(
                (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken)
            );
        }

        return new Success();
    }
}
