using System.Net;
using System.Net.Http.Json;
using EcoData.Common.Http.Helpers;
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
            $"api/organizations{queryString}",
            cancellationToken
        )!;
    }

    public IAsyncEnumerable<MyOrganizationDto> GetMyOrganizationsAsync(
        CancellationToken cancellationToken = default
    )
    {
        return httpClient.GetFromJsonAsAsyncEnumerable<MyOrganizationDto>(
            "api/organizations/my",
            cancellationToken
        )!;
    }

    public async Task<
        OneOf<OrganizationDtoForDetail, NotFoundError, UnauthorizedError>
    > GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"api/organizations/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return new UnauthorizedError();

        var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForDetail>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<OrganizationDtoForCreated, ValidationError, ApiError>> CreateAsync(
        OrganizationDtoForCreate dto,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/organizations",
            dto,
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.BadRequest)
            return await response.Content.ReadFromJsonAsync<ValidationError>(cancellationToken)
                ?? new ValidationError();

        if (!response.IsSuccessStatusCode)
            return new ApiError(
                (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken)
            );

        var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForCreated>(
            cancellationToken
        );
        return result!;
    }

    public async Task<
        OneOf<OrganizationDtoForDetail, NotFoundError, ValidationError, ApiError>
    > UpdateAsync(
        Guid id,
        OrganizationDtoForUpdate dto,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PutAsJsonAsync(
            $"api/organizations/{id}",
            dto,
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError();

        if (response.StatusCode == HttpStatusCode.BadRequest)
            return await response.Content.ReadFromJsonAsync<ValidationError>(cancellationToken)
                ?? new ValidationError();

        if (!response.IsSuccessStatusCode)
            return new ApiError(
                (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken)
            );

        var result = await response.Content.ReadFromJsonAsync<OrganizationDtoForDetail>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<Success, NotFoundError, ConflictError, ApiError>> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.DeleteAsync($"api/organizations/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new NotFoundError();

        if (response.StatusCode == HttpStatusCode.Conflict)
            return new ConflictError(await response.Content.ReadAsStringAsync(cancellationToken));

        if (!response.IsSuccessStatusCode)
            return new ApiError(
                (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken)
            );

        return new Success();
    }
}
