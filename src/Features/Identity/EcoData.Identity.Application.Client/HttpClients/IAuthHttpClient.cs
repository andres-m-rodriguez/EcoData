using EcoData.Common.Problems.Contracts;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using OneOf;
using OneOf.Types;

namespace EcoData.Identity.Application.Client.HttpClients;

public interface IAuthHttpClient
{
    Task<OneOf<LoginResponse, ProblemDetail>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<OneOf<UserInfo, ProblemDetail>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<OneOf<UserInfo, ProblemDetail>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<OneOf<UserInfo, ProblemDetail>> UpdateEmailAsync(UpdateEmailRequest request, CancellationToken cancellationToken = default);
    Task<OneOf<Success, ProblemDetail>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
