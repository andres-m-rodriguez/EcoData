using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using EcoData.Identity.Contracts.Results;

namespace EcoData.Identity.Application.Client.HttpClients;

public interface IAuthHttpClient
{
    Task<AuthResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    );
    Task<AuthResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    );
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
