using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Parameters;
using OneOf;
using OneOf.Types;

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

    public async Task<OneOf<int, RequestFailed>> GetOrganizationCountAsync(
        OrganizationParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = new QueryStringBuilder()
            .Add("search", parameters.Search)
            .Build();

        try
        {
            var response = await httpClient.GetAsync(
                $"organization/organizations/count{queryString}",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            return await response.Content.ReadFromJsonAsync<int>(cancellationToken);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
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

    public async Task<OneOf<OrganizationDtoForDetail, RequestFailed>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"organization/organizations/{id}",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForDetail>(cancellationToken);
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

    public async Task<OneOf<OrganizationDtoForDetail, RequestFailed>> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"organization/organizations/by-slug/{Uri.EscapeDataString(slug)}",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForDetail>(cancellationToken);
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

    public async Task<OneOf<OrganizationDtoForCreated, RequestFailed>> CreateAsync(
        OrganizationDtoForCreate dto,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "organization/organizations",
                dto,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForCreated>(cancellationToken);
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

    public async Task<OneOf<OrganizationDtoForDetail, RequestFailed>> UpdateAsync(
        Guid id,
        OrganizationDtoForUpdate dto,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync(
                $"organization/organizations/{id}",
                dto,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForDetail>(cancellationToken);
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

    public async Task<OneOf<Success, RequestFailed>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.DeleteAsync(
                $"organization/organizations/{id}",
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
