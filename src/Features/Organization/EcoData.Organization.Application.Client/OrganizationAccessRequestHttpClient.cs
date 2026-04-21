using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Pagination;
using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Parameters;
using EcoData.Organization.Contracts.Requests;
using OneOf;

namespace EcoData.Organization.Application.Client;

public sealed class OrganizationAccessRequestHttpClient(HttpClient httpClient)
    : IOrganizationAccessRequestHttpClient
{
    public IAsyncEnumerable<OrganizationAccessRequestDto> GetByOrganizationAsync(
        Guid organizationId,
        OrganizationAccessRequestParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var query = new QueryStringBuilder()
            .AddCursorParameters(parameters)
            .Add("status", parameters.Status)
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<OrganizationAccessRequestDto>(
            $"organization/organizations/{organizationId}/access-requests{query}",
            cancellationToken
        )!;
    }

    public async Task<OneOf<OrganizationAccessRequestDto, ProblemDetail>> GetByIdAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync(
            $"organization/organizations/{organizationId}/access-requests/{id}",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationAccessRequestDto>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<OrganizationAccessRequestDto, ProblemDetail>> CreateAsync(
        Guid organizationId,
        CreateOrganizationAccessRequestRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            $"organization/organizations/{organizationId}/access-requests",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationAccessRequestDto>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<OrganizationAccessRequestDto, ProblemDetail>> UpdateStatusAsync(
        Guid organizationId,
        Guid id,
        UpdateOrganizationAccessRequestStatusRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PutAsJsonAsync(
            $"organization/organizations/{organizationId}/access-requests/{id}/status",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationAccessRequestDto>(
            cancellationToken
        );
        return result!;
    }

    public IAsyncEnumerable<OrganizationAccessRequestDto> GetMyRequestsAsync(
        OrganizationAccessRequestParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var query = new QueryStringBuilder()
            .AddCursorParameters(parameters)
            .Add("status", parameters.Status)
            .Add("search", parameters.Search)
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<OrganizationAccessRequestDto>(
            $"organization/me/access-requests{query}",
            cancellationToken
        )!;
    }

    public async Task<OneOf<OrganizationAccessRequestDto, ProblemDetail>> CancelMyRequestAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PostAsync(
            $"organization/me/access-requests/{id}/cancel",
            null,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationAccessRequestDto>(
            cancellationToken
        );
        return result!;
    }
}
