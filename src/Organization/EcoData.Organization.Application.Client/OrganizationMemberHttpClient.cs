using System.Net;
using System.Net.Http.Json;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Errors;
using EcoData.Organization.Contracts.Parameters;
using EcoData.Common.Http.Helpers;
using EcoData.Common.Pagination;
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
            $"api/organizations/{organizationId}/members{query}",
            cancellationToken
        )!;
    }

    public async Task<OneOf<OrganizationMemberDto, NotFoundError>> GetAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.GetAsync(
            $"api/organizations/{organizationId}/members/{userId}",
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundError();
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationMemberDto>(
            cancellationToken
        );
        return result!;
    }

    public async Task<
        OneOf<OrganizationMemberDto, NotFoundError, ConflictError, ApiError>
    > CreateAsync(
        Guid organizationId,
        AddMemberRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PostAsJsonAsync(
            $"api/organizations/{organizationId}/members",
            request,
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundError();
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ConflictError(message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ApiError(
                (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken)
            );
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationMemberDto>(
            cancellationToken
        );
        return result!;
    }

    public async Task<
        OneOf<OrganizationMemberDto, NotFoundError, ValidationError, ApiError>
    > UpdateAsync(
        Guid organizationId,
        Guid userId,
        UpdateMemberRoleRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.PutAsJsonAsync(
            $"api/organizations/{organizationId}/members/{userId}",
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
            return new ValidationError([new ValidationFailure("Role", message)]);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ApiError(
                (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken)
            );
        }

        var result = await response.Content.ReadFromJsonAsync<OrganizationMemberDto>(
            cancellationToken
        );
        return result!;
    }

    public async Task<OneOf<Success, NotFoundError, ValidationError, ApiError>> DeleteAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        var response = await httpClient.DeleteAsync(
            $"api/organizations/{organizationId}/members/{userId}",
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundError();
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ValidationError([new ValidationFailure("Member", message)]);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ApiError(
                (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken)
            );
        }

        return new Success();
    }
}
