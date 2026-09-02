using System.Net.Http.Json;
using EcoData.Common.Problems.Contracts;
using EcoData.Identity.Contracts.Errors;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using EcoData.Organization.Contracts.Dtos;
using OneOf;
using OneOf.Types;

namespace FaunaFinder.Client.Services.Account;

public sealed class AccountHttpClient(HttpClient httpClient) : IAccountHttpClient
{
    public async Task<OneOf<FaunaFinderSignupResponse, ValidationFailed, RequestFailed>> SignupAsync(
        FaunaFinderSignupRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "/account/signup",
                request,
                cancellationToken
            );
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                if (problem?.Errors is { Count: > 0 } errors)
                    return new ValidationFailed(errors);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var signupResponse = await response.Content.ReadFromJsonAsync<FaunaFinderSignupResponse>(
                cancellationToken
            );
            if (signupResponse is null)
                return new RequestFailed((int)response.StatusCode, "Empty response from server.");
            return signupResponse;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<UserInfo, ValidationFailed, RequestFailed>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "/account/login",
                request,
                cancellationToken
            );
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                if (problem?.Errors is { Count: > 0 } errors)
                    return new ValidationFailed(errors);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var user = await response.Content.ReadFromJsonAsync<UserInfo>(cancellationToken);
            if (user is null)
                return new RequestFailed((int)response.StatusCode, "Empty response from server.");
            return user;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<Success, RequestFailed>> LogoutAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PostAsync("/account/logout", null, cancellationToken);
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

    // Best-effort auth-state probe: any failure (offline, non-2xx) reads as
    // "not signed in"; only transport exceptions are swallowed.
    public async Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("/account/me", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<UserInfo?>(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<OneOf<List<OrganizationAccessRequestDto>, RequestFailed>> GetAccessRequestsAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync("/account/access-requests", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var requests = await response.Content.ReadFromJsonAsync<
                List<OrganizationAccessRequestDto>
            >(cancellationToken);
            if (requests is null)
                return new RequestFailed((int)response.StatusCode, "Empty response from server.");
            return requests;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<FaunaFinderOrganizationDto, RequestFailed>> GetOrganizationAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync("/account/organization", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var organization = await response.Content.ReadFromJsonAsync<FaunaFinderOrganizationDto>(
                cancellationToken
            );
            if (organization is null)
                return new RequestFailed((int)response.StatusCode, "Empty response from server.");
            return organization;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<UserPermissionsDto, RequestFailed>> GetPermissionsAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync("/account/permissions", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var permissions = await response.Content.ReadFromJsonAsync<UserPermissionsDto>(
                cancellationToken
            );
            if (permissions is null)
                return new RequestFailed((int)response.StatusCode, "Empty response from server.");
            return permissions;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
