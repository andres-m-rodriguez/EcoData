using System.Security.Claims;
using EcoData.Identity.Contracts.Parameters;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using EcoData.Identity.Contracts.Results;

namespace EcoData.Identity.Application.Services;

public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    );
    Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    );
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<UserInfo?> GetCurrentUserAsync(
        ClaimsPrincipal? principal,
        CancellationToken cancellationToken = default
    );
    IAsyncEnumerable<UserInfo> GetUsersAsync(
        UserParameters parameters,
        CancellationToken cancellationToken = default
    );
}
