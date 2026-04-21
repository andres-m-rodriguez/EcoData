using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Pagination;
using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Errors;
using EcoData.Organization.Contracts.Parameters;
using OneOf;

namespace EcoData.Organization.Application.Client;

public sealed class OrganizationMemberHttpClient(HttpClient httpClient)
    : IOrganizationMemberHttpClient
{
    public IAsyncEnumerable<OrganizationMemberDto> GetAllAsync(
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

    public async Task<OneOf<OrganizationMemberDto, ProblemDetail>> GetAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync(
            $"organization/organizations/{organizationId}/members/{userId}",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationMemberDto>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<OrganizationMemberDto, ProblemDetail>> UpdateAsync(
        Guid organizationId,
        Guid userId,
        UpdateMemberRoleRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PutAsJsonAsync(
            $"organization/organizations/{organizationId}/members/{userId}",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationMemberDto>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<Success, ProblemDetail>> DeleteAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.DeleteAsync(
            $"organization/organizations/{organizationId}/members/{userId}",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        return new Success();
    }
}
