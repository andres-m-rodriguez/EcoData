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

    public async Task<OneOf<OrganizationAccessRequestDto, RequestFailed>> GetByIdAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"organization/organizations/{organizationId}/access-requests/{id}",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<OrganizationAccessRequestDto>(
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

    public async Task<OneOf<OrganizationAccessRequestDto, RequestFailed>> CreateAsync(
        Guid organizationId,
        CreateOrganizationAccessRequestRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"organization/organizations/{organizationId}/access-requests",
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<OrganizationAccessRequestDto>(
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

    public async Task<OneOf<OrganizationAccessRequestDto, RequestFailed>> UpdateStatusAsync(
        Guid organizationId,
        Guid id,
        UpdateOrganizationAccessRequestStatusRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync(
                $"organization/organizations/{organizationId}/access-requests/{id}/status",
                request,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<OrganizationAccessRequestDto>(
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

    public async Task<OneOf<OrganizationAccessRequestDto, RequestFailed>> CancelMyRequestAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PostAsync(
                $"organization/me/access-requests/{id}/cancel",
                null,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<OrganizationAccessRequestDto>(
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
}
