using System.Net.Http.Json;
using EcoData.Common.Problems.Contracts;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using OneOf;

namespace EcoData.Identity.Application.Client.HttpClients;

public sealed class AuthHttpClient(HttpClient httpClient) : IAuthHttpClient
{
    public async Task<OneOf<LoginResponse, ProblemDetail>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/login", request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
            return loginResponse!;
        }

        return await response.ReadProblemAsync(cancellationToken);
    }

    public async Task<OneOf<UserInfo, ProblemDetail>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/register", request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var user = await response.Content.ReadFromJsonAsync<UserInfo>(cancellationToken);
            return user!;
        }

        return await response.ReadProblemAsync(cancellationToken);
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
