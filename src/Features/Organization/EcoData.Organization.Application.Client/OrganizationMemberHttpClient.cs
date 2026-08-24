using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Pagination;
using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Parameters;
using OneOf;
using OneOf.Types;

namespace EcoData.Organization.Application.Client;

public sealed class OrganizationMemberHttpClient(HttpClient httpClient)
    : IOrganizationMemberHttpClient
{
    public IAsyncEnumerable<OrganizationMemberDto> GetAsync(
        Guid organizationId,
        OrganizationMemberParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var query = new QueryStringBuilder()
            .AddCursorParameters(parameters)
            .Add("search", parameters.Search)
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<OrganizationMemberDto>(
            $"organization/organizations/{organizationId}/members{query}",
            cancellationToken
        )!;
    }

    public async Task<OneOf<OrganizationMemberDto, RequestFailed>> GetAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"organization/organizations/{organizationId}/members/{userId}",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<OrganizationMemberDto>(
                cancellationToken
            );
            if (result is null)
            {
                return new RequestFailed(
                    (int)response.StatusCode,
                    "The server returned an empty response."
                );
            }

            return result;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<OrganizationMemberDto, RequestFailed>> UpdateAsync(
        Guid organizationId,
        Guid userId,
        UpdateMemberRoleRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync(
                $"organization/organizations/{organizationId}/members/{userId}",
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<OrganizationMemberDto>(
                cancellationToken
            );
            if (result is null)
            {
                return new RequestFailed(
                    (int)response.StatusCode,
                    "The server returned an empty response."
                );
            }

            return result;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<Success, RequestFailed>> DeleteAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.DeleteAsync(
                $"organization/organizations/{organizationId}/members/{userId}",
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
