using System.Net.Http.Json;
using System.Text.Json;
using EcoData.Common.Problems.Contracts;
using EcoData.Identity.Contracts.Errors;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using OneOf;
using OneOf.Types;

namespace EcoData.Identity.Application.Client.HttpClients;

public sealed class AuthHttpClient(HttpClient httpClient) : IAuthHttpClient
{
    public async Task<OneOf<LoginResponse, ValidationFailed, RequestFailed>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "/identity/auth/login",
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

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(
                cancellationToken
            );
            if (loginResponse is null)
                return new RequestFailed((int)response.StatusCode, "Empty response from server.");
            return loginResponse;
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }

    public async Task<OneOf<UserInfo, ValidationFailed, RequestFailed>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "/identity/auth/register",
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
            var response = await httpClient.PostAsync(
                "/identity/auth/logout",
                null,
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

    // Best-effort auth-state probe: any failure (offline, non-2xx, malformed body) reads
    // as "not signed in" — transport and JSON exceptions are swallowed, nothing else.
    public async Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("/identity/auth/me", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<UserInfo?>(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<OneOf<UserInfo, ValidationFailed, RequestFailed>> UpdateProfileAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PatchAsJsonAsync(
                "/identity/auth/profile",
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

    public async Task<OneOf<UserInfo, ValidationFailed, RequestFailed>> UpdateEmailAsync(
        UpdateEmailRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PatchAsJsonAsync(
                "/identity/auth/email",
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

    public async Task<OneOf<Success, ValidationFailed, RequestFailed>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.PatchAsJsonAsync(
                "/identity/auth/password",
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

            return new Success();
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
