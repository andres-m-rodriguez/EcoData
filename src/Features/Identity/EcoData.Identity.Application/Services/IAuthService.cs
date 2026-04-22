using System.Security.Claims;
using EcoData.Identity.Contracts.Errors;
using EcoData.Identity.Contracts.Parameters;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using OneOf;
using OneOf.Types;

namespace EcoData.Identity.Application.Services;

public interface IAuthService
{
    Task<OneOf<UserInfo, EmailAlreadyExists, ValidationFailed>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    );
    Task<OneOf<LoginResponse, InvalidCredentials, AccountLocked, TooManyRequests, ValidationFailed>> LoginAsync(
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
    Task<OneOf<UserInfo, ValidationFailed>> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default
    );
    Task<OneOf<UserInfo, InvalidPassword, EmailAlreadyExists, ValidationFailed>> UpdateEmailAsync(
        Guid userId,
        UpdateEmailRequest request,
        CancellationToken cancellationToken = default
    );
    Task<OneOf<Success, InvalidPassword, ValidationFailed>> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default
    );
}
