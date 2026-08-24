using System.Net.Http.Json;
using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Requests;
using OneOf;
using OneOf.Types;

namespace EcoData.Organization.Application.Client;

public sealed class OrganizationRoleHttpClient(HttpClient httpClient) : IOrganizationRoleHttpClient
{
    public async Task<OneOf<IReadOnlyList<OrganizationRoleDto>, RequestFailed>> GetListAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"organization/organizations/{organizationId}/roles",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<List<OrganizationRoleDto>>(
                cancellationToken
            );

            return result ?? [];
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<OrganizationRoleDto, RequestFailed>> CreateAsync(
        Guid organizationId,
        OrganizationRoleRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"organization/organizations/{organizationId}/roles",
                request,
                cancellationToken
            );

            return await ReadRoleAsync(response, cancellationToken);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<OrganizationRoleDto, RequestFailed>> UpdateAsync(
        Guid organizationId,
        Guid roleId,
        OrganizationRoleRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync(
                $"organization/organizations/{organizationId}/roles/{roleId}",
                request,
                cancellationToken
            );

            return await ReadRoleAsync(response, cancellationToken);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<Success, RequestFailed>> DeleteAsync(
        Guid organizationId,
        Guid roleId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.DeleteAsync(
                $"organization/organizations/{organizationId}/roles/{roleId}",
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

    private static async Task<OneOf<OrganizationRoleDto, RequestFailed>> ReadRoleAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        if (!response.IsSuccessStatusCode)
        {
            var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
            return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationRoleDto>(cancellationToken);
        if (result is null)
        {
            return new RequestFailed(
                (int)response.StatusCode,
                "The server returned an empty response."
            );
        }

        return result;
    }
}
