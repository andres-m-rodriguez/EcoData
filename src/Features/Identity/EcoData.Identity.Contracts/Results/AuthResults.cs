using EcoData.Identity.Contracts.Errors;
using EcoData.Identity.Contracts.Responses;
using OneOf;

namespace EcoData.Identity.Contracts.Results;

[GenerateOneOf]
public partial class RegisterResult : OneOfBase<UserInfo, EmailAlreadyExists, ValidationFailed>;

[GenerateOneOf]
public partial class LoginResult
    : OneOfBase<UserInfo, InvalidCredentials, AccountLocked, ValidationFailed>;

public record AuthResult(bool Success, UserInfo? User = null, string? Error = null);

public record SensorAuthResult(
    bool Success,
    string? AccessToken = null,
    DateTimeOffset? ExpiresAt = null,
    string? Error = null
);
