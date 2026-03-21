using System.Security.Claims;
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
        var group = app.MapGroup("/api/auth").WithTags("User Auth");

        group
            .MapPost(
                "/register",
                async (RegisterRequest request, IAuthService authService, CancellationToken ct) =>
                {
                    var result = await authService.RegisterAsync(request, ct);

                    return result.Match<Results<Ok<UserInfo>, ProblemHttpResult>>(
                        userInfo => TypedResults.Ok(userInfo),
                        _ => TypedResults.Problem(
                            detail: "An account with this email already exists",
                            statusCode: StatusCodes.Status409Conflict
                        ),
                        validationFailed => TypedResults.Problem(
                            detail: string.Join(", ", validationFailed.Errors),
                            statusCode: StatusCodes.Status400BadRequest
                        )
                    );
                }
            )
            .WithName("Register");

        group
            .MapPost(
                "/login",
                async (LoginRequest request, IAuthService authService, CancellationToken ct) =>
                {
                    var result = await authService.LoginAsync(request, ct);

                    return result.Match<Results<Ok<UserInfo>, ProblemHttpResult>>(
                        userInfo => TypedResults.Ok(userInfo),
                        _ => TypedResults.Problem(
                            detail: "Invalid email or password",
                            statusCode: StatusCodes.Status401Unauthorized
                        ),
                        _ => TypedResults.Problem(
                            detail: "Your account has been locked. Please try again later.",
                            statusCode: 423
                        ),
                        tooMany => TypedResults.Problem(
                            detail: tooMany.RetryAfterMinutes > 1
                                ? $"Too many login attempts. Please try again in {tooMany.RetryAfterMinutes} minutes."
                                : "Too many login attempts. Please try again in 1 minute.",
                            statusCode: StatusCodes.Status429TooManyRequests
                        ),
                        validationFailed => TypedResults.Problem(
                            detail: string.Join(", ", validationFailed.Errors),
                            statusCode: StatusCodes.Status400BadRequest
                        )
                    );
                }
            )
            .WithName("Login")
            .RequireRateLimiting(LoginRateLimiterExtensions.LoginRateLimiterPolicy);

        group
            .MapGet(
                "/me",
                async (ClaimsPrincipal user, IAuthService authService, CancellationToken ct) =>
                    TypedResults.Ok<UserInfo?>(await authService.GetCurrentUserAsync(user, ct))
            )
            .WithName("GetCurrentUser");

        group
            .MapPost(
                "/logout",
                async (IAuthService authService, CancellationToken ct) =>
                {
                    await authService.LogoutAsync(ct);
                    return TypedResults.Ok();
                }
            )
            .WithName("Logout")
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
