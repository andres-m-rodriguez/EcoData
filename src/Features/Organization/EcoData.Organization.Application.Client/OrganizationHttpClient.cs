using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Errors;
using EcoData.Organization.Contracts.Parameters;
using OneOf;

namespace EcoData.Organization.Application.Client;

public sealed class OrganizationHttpClient(HttpClient httpClient) : IOrganizationHttpClient
{
    public IAsyncEnumerable<OrganizationDtoForList> GetOrganizationsAsync(
        OrganizationParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = new QueryStringBuilder()
            .Add("pageSize", parameters.PageSize != 20 ? parameters.PageSize : null)
            .Add("cursor", parameters.Cursor)
            .Add("search", parameters.Search)
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<OrganizationDtoForList>(
            $"organization/organizations{queryString}",
            cancellationToken
        )!;
    }

    public Task<int> GetOrganizationCountAsync(
        OrganizationParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = new QueryStringBuilder()
            .Add("search", parameters.Search)
            .Build();

        return httpClient.GetFromJsonAsync<int>(
            $"organization/organizations/count{queryString}",
            cancellationToken
        )!;
    }

    public IAsyncEnumerable<MyOrganizationDto> GetMyOrganizationsAsync(
        CancellationToken cancellationToken = default
    )
    {
        return httpClient.GetFromJsonAsAsyncEnumerable<MyOrganizationDto>(
            "organization/organizations/my",
            cancellationToken
        )!;
    }

    public async Task<OneOf<OrganizationDtoForDetail, ProblemDetail>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync($"organization/organizations/{id}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForDetail>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<OrganizationDtoForDetail, ProblemDetail>> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync(
            $"organization/organizations/by-slug/{Uri.EscapeDataString(slug)}",
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForDetail>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<OrganizationDtoForCreated, ProblemDetail>> CreateAsync(
        OrganizationDtoForCreate dto,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            "organization/organizations",
            dto,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForCreated>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<OrganizationDtoForDetail, ProblemDetail>> UpdateAsync(
        Guid id,
        OrganizationDtoForUpdate dto,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PutAsJsonAsync(
            $"organization/organizations/{id}",
            dto,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForDetail>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<Success, ProblemDetail>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.DeleteAsync($"organization/organizations/{id}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return await response.ReadProblemAsync(cancellationToken);
        }

        return new Success();
    }
}
