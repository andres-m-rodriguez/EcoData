using EcoData.Common.Problems.Contracts;
using EcoData.Identity.Contracts.Errors;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using OneOf;
using OneOf.Types;

namespace EcoData.Identity.Application.Client.HttpClients;

public interface IAuthHttpClient
{
    Task<OneOf<LoginResponse, ValidationFailed, RequestFailed>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    );
    Task<OneOf<UserInfo, ValidationFailed, RequestFailed>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default
    );
    Task<OneOf<Success, RequestFailed>> LogoutAsync(CancellationToken cancellationToken = default);
    Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<OneOf<UserInfo, ValidationFailed, RequestFailed>> UpdateProfileAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default
    );
    Task<OneOf<UserInfo, ValidationFailed, RequestFailed>> UpdateEmailAsync(
        UpdateEmailRequest request,
        CancellationToken cancellationToken = default
    );
    Task<OneOf<Success, ValidationFailed, RequestFailed>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default
    );
}
