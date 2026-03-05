using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;

namespace EcoData.Identity.Application.Client;

public interface IAuthHttpClient
{
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}

public record AuthResult(bool Success, UserInfo? User = null, string? Error = null);
