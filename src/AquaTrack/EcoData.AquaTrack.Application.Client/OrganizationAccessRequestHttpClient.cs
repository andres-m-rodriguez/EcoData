using System.Net;
using System.Net.Http.Json;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Errors;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.Contracts.Requests;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Pagination;
using OneOf;

namespace EcoData.AquaTrack.Application.Client;

public sealed class OrganizationAccessRequestHttpClient(HttpClient httpClient) : IOrganizationAccessRequestHttpClient
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
            $"api/organizations/{organizationId}/access-requests{query}",
            cancellationToken
        )!;
    }

    public async Task<OneOf<OrganizationAccessRequestDto, NotFoundError>> GetByIdAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync(
            $"api/organizations/{organizationId}/access-requests/{id}",
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundError();
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationAccessRequestDto>(cancellationToken);
        return result!;
    }

    public async Task<OneOf<OrganizationAccessRequestDto, ConflictError, ApiError>> CreateAsync(
        Guid organizationId,
        CreateOrganizationAccessRequestRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/organizations/{organizationId}/access-requests",
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
            return new ApiError((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationAccessRequestDto>(cancellationToken);
        return result!;
    }

    public async Task<OneOf<OrganizationAccessRequestDto, NotFoundError, ValidationError, ApiError>> UpdateStatusAsync(
        Guid organizationId,
        Guid id,
        UpdateOrganizationAccessRequestStatusRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PutAsJsonAsync(
            $"api/organizations/{organizationId}/access-requests/{id}/status",
            request,
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundError();
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ValidationError([new ValidationFailure("Status", message)]);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ApiError((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationAccessRequestDto>(cancellationToken);
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
            $"api/me/access-requests{query}",
            cancellationToken
        )!;
    }

    public async Task<OneOf<OrganizationAccessRequestDto, NotFoundError, ValidationError, ApiError>> CancelMyRequestAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PostAsync($"api/me/access-requests/{id}/cancel", null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundError();
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ValidationError([new ValidationFailure("Request", message)]);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ApiError((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationAccessRequestDto>(cancellationToken);
        return result!;
    }
}
