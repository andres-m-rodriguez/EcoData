using System.Security.Claims;
using EcoData.Identity.Api.Authentication;
using EcoData.Identity.Api.RateLimiting;
using EcoData.Identity.Application.Services;
using EcoData.Identity.Contracts.Authorization;
using EcoData.Identity.Contracts.Parameters;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Identity.Api.Endpoints;

public static class UserAuthEndpoints
{
    public static IEndpointRouteBuilder MapUserAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/identity/auth").WithTags("User Auth");

        group
            .MapPost(
                "/register",
                async (RegisterRequest request, IAuthService authService, CancellationToken ct) =>
                {
                    var result = await authService.RegisterAsync(request, ct);

                    return result.Match<Results<Ok<UserInfo>, ProblemHttpResult, ValidationProblem>>(
                        userInfo => TypedResults.Ok(userInfo),
                        _ =>
                            TypedResults.Problem(
                                detail: "An account with this email already exists",
                                statusCode: StatusCodes.Status409Conflict
                            ),
                        validationFailed => TypedResults.ValidationProblem(validationFailed.Errors)
                    );
                }
            )
            .WithName("Register");

        group
            .MapPost(
                "/login",
                async (
                    LoginRequest request,
                    IAuthService authService,
                    HttpContext httpContext,
                    CancellationToken ct
                ) =>
                {
                    var result = await authService.LoginAsync(request, ct);

                    return result.Match<Results<Ok<LoginResponse>, ProblemHttpResult, ValidationProblem>>(
                        loginResponse =>
                        {
                            httpContext.Response.Cookies.Append(
                                UserJwtAuthentication.CookieName,
                                loginResponse.Token,
                                new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = true,
                                    SameSite = SameSiteMode.Strict,
                                    Expires = loginResponse.ExpiresAt,
                                }
                            );
                            return TypedResults.Ok(loginResponse);
                        },
                        _ =>
                            TypedResults.Problem(
                                detail: "Invalid email or password",
                                statusCode: StatusCodes.Status401Unauthorized
                            ),
                        _ =>
                            TypedResults.Problem(
                                detail: "Your account has been locked. Please try again later.",
                                statusCode: 423
                            ),
                        tooMany =>
                            TypedResults.Problem(
                                detail: tooMany.RetryAfterMinutes > 1
                                    ? $"Too many login attempts. Please try again in {tooMany.RetryAfterMinutes} minutes."
                                    : "Too many login attempts. Please try again in 1 minute.",
                                statusCode: StatusCodes.Status429TooManyRequests
                            ),
                        validationFailed => TypedResults.ValidationProblem(validationFailed.Errors)
                    );
                }
            )
            .WithName("Login")
            .RequireRateLimiting(LoginRateLimiterExtensions.LoginRateLimiterPolicy);

        group
            .MapGet(
                "/me",
                // Ok(null) writes a 200 with an empty body, which no JSON reader can parse;
                // "no signed-in user" is a 401, not an empty success.
                async Task<Results<Ok<UserInfo>, UnauthorizedHttpResult>> (
                    ClaimsPrincipal user,
                    IAuthService authService,
                    CancellationToken ct
                ) =>
                    await authService.GetCurrentUserAsync(user, ct) is { } userInfo
                        ? TypedResults.Ok(userInfo)
                        : TypedResults.Unauthorized()
            )
            .WithName("GetCurrentUser");

        group
            .MapPost(
                "/logout",
                (HttpContext httpContext) =>
                {
                    httpContext.Response.Cookies.Delete(UserJwtAuthentication.CookieName);
                    return TypedResults.Ok();
                }
            )
            .WithName("Logout")
            .RequireAuthorization();

        group
            .MapPatch(
                "/profile",
                async Task<Results<Ok<UserInfo>, ProblemHttpResult, ValidationProblem>> (
                    UpdateProfileRequest request,
                    ClaimsPrincipal user,
                    IAuthService authService,
                    CancellationToken ct
                ) =>
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                    {
                        return TypedResults.Problem(
                            detail: "Invalid user",
                            statusCode: StatusCodes.Status401Unauthorized
                        );
                    }

                    var result = await authService.UpdateProfileAsync(userId, request, ct);

                    return result.Match<Results<Ok<UserInfo>, ProblemHttpResult, ValidationProblem>>(
                        userInfo => TypedResults.Ok(userInfo),
                        validationFailed => TypedResults.ValidationProblem(validationFailed.Errors)
                    );
                }
            )
            .WithName("UpdateProfile")
            .RequireAuthorization();

        group
            .MapPatch(
                "/email",
                async Task<Results<Ok<UserInfo>, ProblemHttpResult, ValidationProblem>> (
                    UpdateEmailRequest request,
                    ClaimsPrincipal user,
                    IAuthService authService,
                    CancellationToken ct
                ) =>
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                    {
                        return TypedResults.Problem(
                            detail: "Invalid user",
                            statusCode: StatusCodes.Status401Unauthorized
                        );
                    }

                    var result = await authService.UpdateEmailAsync(userId, request, ct);

                    return result.Match<Results<Ok<UserInfo>, ProblemHttpResult, ValidationProblem>>(
                        userInfo => TypedResults.Ok(userInfo),
                        _ =>
                            TypedResults.Problem(
                                detail: "Invalid password",
                                statusCode: StatusCodes.Status401Unauthorized
                            ),
                        _ =>
                            TypedResults.Problem(
                                detail: "An account with this email already exists",
                                statusCode: StatusCodes.Status409Conflict
                            ),
                        validationFailed => TypedResults.ValidationProblem(validationFailed.Errors)
                    );
                }
            )
            .WithName("UpdateEmail")
            .RequireAuthorization();

        group
            .MapPatch(
                "/password",
                async Task<Results<Ok, ProblemHttpResult, ValidationProblem>> (
                    ChangePasswordRequest request,
                    ClaimsPrincipal user,
                    IAuthService authService,
                    CancellationToken ct
                ) =>
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!Guid.TryParse(userIdClaim, out var userId))
                    {
                        return TypedResults.Problem(
                            detail: "Invalid user",
                            statusCode: StatusCodes.Status401Unauthorized
                        );
                    }

                    var result = await authService.ChangePasswordAsync(userId, request, ct);

                    return result.Match<Results<Ok, ProblemHttpResult, ValidationProblem>>(
                        _ => TypedResults.Ok(),
                        _ =>
                            TypedResults.Problem(
                                detail: "Invalid current password",
                                statusCode: StatusCodes.Status401Unauthorized
                            ),
                        validationFailed => TypedResults.ValidationProblem(validationFailed.Errors)
                    );
                }
            )
            .WithName("ChangePassword")
            .RequireAuthorization();

        // Admin endpoints
        group
            .MapGet(
                "/users",
                (
                    [AsParameters] UserParameters parameters,
                    IAuthService authService,
                    CancellationToken ct
                ) => authService.GetUsersAsync(parameters, ct)
            )
            .WithName("GetUsers")
            .RequireAuthorization(PolicyNames.Admin);

        return app;
    }
}
