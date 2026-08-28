using EcoData.Common.Problems.Contracts;
using EcoData.Identity.Contracts.Errors;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using EcoData.Organization.Contracts.Dtos;
using OneOf;
using OneOf.Types;

namespace FaunaFinder.Client.Services.Account;

public interface IAccountHttpClient
{
    Task<OneOf<FaunaFinderSignupResponse, ValidationFailed, RequestFailed>> SignupAsync(
        FaunaFinderSignupRequest request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<UserInfo, ValidationFailed, RequestFailed>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<Success, RequestFailed>> LogoutAsync(CancellationToken cancellationToken = default);

    Task<UserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<OneOf<List<OrganizationAccessRequestDto>, RequestFailed>> GetAccessRequestsAsync(
        CancellationToken cancellationToken = default
    );
}
