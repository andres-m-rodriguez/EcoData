using EcoData.Common.Problems.Contracts;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using OneOf;

namespace EcoData.Identity.Application.Client.HttpClients;

public interface IAuthHttpClient
{
    Task<OneOf<LoginResponse, ProblemDetail>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<OneOf<UserInfo, ProblemDetail>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
