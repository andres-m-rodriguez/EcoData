using System.Security.Claims;
using EcoData.Identity.Application.Services;
using EcoData.Identity.Contracts.Authorization;
using EcoData.Identity.Contracts.Parameters;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Identity.Api;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group
            .MapPost(
                "/register",
                async (RegisterRequest request, IAuthService authService, CancellationToken ct) =>
                {
                    var result = await authService.RegisterAsync(request, ct);

                    return result.Match<
                        Results<Ok<UserInfo>, BadRequest<IReadOnlyList<string>>, Conflict<string>>
                    >(
                        userInfo => TypedResults.Ok(userInfo),
                        _ => TypedResults.Conflict("An account with this email already exists"),
                        validationFailed => TypedResults.BadRequest(validationFailed.Errors)
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

                    return result.Match<
                        Results<
                            Ok<UserInfo>,
                            BadRequest<IReadOnlyList<string>>,
                            UnauthorizedHttpResult,
                            StatusCodeHttpResult
                        >
                    >(
                        userInfo => TypedResults.Ok(userInfo),
                        _ => TypedResults.Unauthorized(),
                        _ => TypedResults.StatusCode(423), // Locked
                        validationFailed => TypedResults.BadRequest(validationFailed.Errors)
                    );
                }
            )
            .WithName("Login");

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
