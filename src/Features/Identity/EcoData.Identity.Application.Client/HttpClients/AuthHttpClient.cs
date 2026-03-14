using System.Net;
using System.Net.Http.Json;
using EcoData.Identity.Contracts.Errors;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using EcoData.Identity.Contracts.Results;

namespace EcoData.Identity.Application.Client.HttpClients;

public sealed class AuthHttpClient(HttpClient httpClient) : IAuthHttpClient
{
    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/login", request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var user = await response.Content.ReadFromJsonAsync<UserInfo>(cancellationToken);
            return user!;
        }

        return response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => new TooManyRequests(GetRetryAfterMinutes(response)),
            HttpStatusCode.Unauthorized => new InvalidCredentials(),
            (HttpStatusCode)423 => new AccountLocked(),
            HttpStatusCode.BadRequest => new ValidationFailed(
                await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>(cancellationToken) ?? ["Validation failed"]),
            _ => new ValidationFailed(["An error occurred during login"])
        };
    }

    public async Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/register", request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var user = await response.Content.ReadFromJsonAsync<UserInfo>(cancellationToken);
            return user!;
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Conflict => new EmailAlreadyExists(),
            HttpStatusCode.BadRequest => new ValidationFailed(
                await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>(cancellationToken) ?? ["Validation failed"]),
            _ => new ValidationFailed(["An error occurred during registration"])
        };
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        await httpClient.PostAsync("/api/auth/logout", null, cancellationToken);
    }

    public async Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<UserInfo?>("/api/auth/me", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static int GetRetryAfterMinutes(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            var retryAfter = values.FirstOrDefault();
            if (int.TryParse(retryAfter, out var seconds))
            {
                return (int)Math.Ceiling(seconds / 60.0);
            }
        }
        return 2; // Default to 2 minutes
    }
}
