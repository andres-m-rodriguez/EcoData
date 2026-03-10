using System.Net;
using System.Net.Http.Json;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Errors;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.Common.Http.Helpers;
using OneOf;

namespace EcoData.AquaTrack.Application.Client;

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
            $"api/organizations{queryString}",
            cancellationToken
        )!;
    }

    public async Task<OneOf<OrganizationDtoForDetail, NotFoundError>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync($"api/organizations/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError();

        var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForDetail>(cancellationToken);
        return result!;
    }

    public async Task<OneOf<OrganizationDtoForCreated, ValidationError, ApiError>> CreateAsync(
        OrganizationDtoForCreate dto,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PostAsJsonAsync("api/organizations", dto, cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
            return await response.Content.ReadFromJsonAsync<ValidationError>(cancellationToken) ?? new ValidationError();

        if (!response.IsSuccessStatusCode)
            return new ApiError((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));

        var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForCreated>(cancellationToken);
        return result!;
    }

    public async Task<OneOf<OrganizationDtoForDetail, NotFoundError, ValidationError, ApiError>> UpdateAsync(
        Guid id,
        OrganizationDtoForUpdate dto,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PutAsJsonAsync($"api/organizations/{id}", dto, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError();

        if (response.StatusCode == HttpStatusCode.BadRequest)
            return await response.Content.ReadFromJsonAsync<ValidationError>(cancellationToken) ?? new ValidationError();

        if (!response.IsSuccessStatusCode)
            return new ApiError((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));

        var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForDetail>(cancellationToken);
        return result!;
    }

    public async Task<OneOf<Success, NotFoundError, ApiError>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.DeleteAsync($"api/organizations/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError();

        if (!response.IsSuccessStatusCode)
            return new ApiError((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));

        return new Success();
    }
}
