using EcoData.Identity.Contracts.Errors;
using EcoData.Identity.Contracts.Responses;
using OneOf;

namespace EcoData.Identity.Contracts.Results;

[GenerateOneOf]
public partial class RegisterResult : OneOfBase<UserInfo, EmailAlreadyExists, ValidationFailed>;

[GenerateOneOf]
public partial class LoginResult
    : OneOfBase<UserInfo, InvalidCredentials, AccountLocked, ValidationFailed>;
