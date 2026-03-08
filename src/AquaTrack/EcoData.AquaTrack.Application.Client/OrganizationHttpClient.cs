using System.Net.Http.Json;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.Common.Http.Helpers;

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

    public async Task<OrganizationDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync($"api/organizations/{id}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<OrganizationDtoForDetail>(cancellationToken);
    }

    public async Task<OrganizationDtoForCreated> CreateAsync(
        OrganizationDtoForCreate dto,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PostAsJsonAsync("api/organizations", dto, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<OrganizationDtoForCreated>(cancellationToken))!;
    }

    public async Task<OrganizationDtoForDetail?> UpdateAsync(
        Guid id,
        OrganizationDtoForUpdate dto,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PutAsJsonAsync($"api/organizations/{id}", dto, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<OrganizationDtoForDetail>(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"api/organizations/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
