using System.Net;
using System.Net.Http.Json;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using EcoData.Identity.Contracts.Results;

namespace EcoData.Identity.Application.Client.HttpClients;

public sealed class AuthHttpClient(HttpClient httpClient) : IAuthHttpClient
{
    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/login", request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var user = await response.Content.ReadFromJsonAsync<UserInfo>(cancellationToken);
            return new AuthResult(true, user);
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfterSeconds = GetRetryAfterSeconds(response);
            var minutes = (int)Math.Ceiling(retryAfterSeconds / 60.0);
            var error = minutes > 1
                ? $"Too many login attempts. Please try again in {minutes} minutes."
                : "Too many login attempts. Please try again in 1 minute.";
            return new AuthResult(false, Error: error);
        }

        var error2 = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Invalid email or password",
            HttpStatusCode.Forbidden => "Your account is pending approval",
            (HttpStatusCode)423 => "Your account has been locked. Please try again later.",
            _ => "An error occurred during login"
        };

        return new AuthResult(false, Error: error2);
    }

    private static int GetRetryAfterSeconds(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Retry-After", out var values))
        {
            var retryAfter = values.FirstOrDefault();
            if (int.TryParse(retryAfter, out var seconds))
            {
                return seconds;
            }
        }
        return 120; // Default to 2 minutes
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/register", request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var user = await response.Content.ReadFromJsonAsync<UserInfo>(cancellationToken);
            return new AuthResult(true, user);
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            return new AuthResult(false, Error: message.Trim('"'));
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var errors = await response.Content.ReadFromJsonAsync<IReadOnlyList<string>>(cancellationToken);
            return new AuthResult(false, Error: string.Join(", ", errors ?? ["Validation failed"]));
        }

        return new AuthResult(false, Error: "An error occurred during registration");
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
}
