using EcoData.Identity.Contracts.Responses;

namespace FaunaFinder.Client.Services.Account;

public sealed record FaunaFinderSignupRequest(
    string Email,
    string DisplayName,
    string Password,
    string ConfirmPassword,
    bool IsStudent
);

public sealed record FaunaFinderSignupResponse(UserInfo User, bool AccessRequestSubmitted);
