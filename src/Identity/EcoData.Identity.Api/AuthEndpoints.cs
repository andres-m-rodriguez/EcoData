using System.Security.Claims;
using EcoData.Identity.Application.Services;
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

        group.MapPost("/register", Register).WithName("Register");
        group.MapPost("/login", Login).WithName("Login");
        group.MapGet("/me", GetCurrentUser).WithName("GetCurrentUser");
        group.MapPost("/logout", Logout).WithName("Logout").RequireAuthorization();

        // Admin endpoints
        group.MapGet("/users", GetUsers).WithName("GetUsers").RequireAuthorization("Admin");
        group.MapGet("/access-requests", GetAccessRequests).WithName("GetAccessRequests").RequireAuthorization("Admin");
        group.MapPut("/access-requests/{id:guid}/status", UpdateAccessRequestStatus)
            .WithName("UpdateAccessRequestStatus")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Results<Ok<UserInfo>, BadRequest<IReadOnlyList<string>>, Conflict<string>>> Register(
        RegisterRequest request,
        IAuthService authService,
        CancellationToken ct
    )
    {
        var result = await authService.RegisterAsync(request, ct);

        return result.Match<Results<Ok<UserInfo>, BadRequest<IReadOnlyList<string>>, Conflict<string>>>(
            userInfo => TypedResults.Ok(userInfo),
            _ => TypedResults.Conflict("An account with this email already exists"),
            _ => TypedResults.Conflict("A registration request with this email is already pending"),
            validationFailed => TypedResults.BadRequest(validationFailed.Errors)
        );
    }

    private static async Task<Results<Ok<UserInfo>, BadRequest<IReadOnlyList<string>>, UnauthorizedHttpResult, StatusCodeHttpResult>> Login(
        LoginRequest request,
        IAuthService authService,
        CancellationToken ct
    )
    {
        var result = await authService.LoginAsync(request, ct);

        return result.Match<Results<Ok<UserInfo>, BadRequest<IReadOnlyList<string>>, UnauthorizedHttpResult, StatusCodeHttpResult>>(
            userInfo => TypedResults.Ok(userInfo),
            _ => TypedResults.Unauthorized(),
            _ => TypedResults.StatusCode(423), // Locked
            _ => TypedResults.StatusCode(403), // Forbidden - account not approved
            validationFailed => TypedResults.BadRequest(validationFailed.Errors)
        );
    }

    private static async Task<Ok<UserInfo?>> GetCurrentUser(
        ClaimsPrincipal user,
        IAuthService authService,
        CancellationToken ct
    ) => TypedResults.Ok<UserInfo?>(await authService.GetCurrentUserAsync(user, ct));

    private static async Task<Ok> Logout(
        IAuthService authService,
        CancellationToken ct
    )
    {
        await authService.LogoutAsync(ct);
        return TypedResults.Ok();
    }

    private static IAsyncEnumerable<UserInfo> GetUsers(
        [AsParameters] UserParameters parameters,
        IAuthService authService,
        CancellationToken ct
    ) => authService.GetUsersAsync(parameters, ct);

    private static IAsyncEnumerable<AccessRequestResponse> GetAccessRequests(
        [AsParameters] AccessRequestParameters parameters,
        IAuthService authService,
        CancellationToken ct
    ) => authService.GetAccessRequestsAsync(parameters, ct);

    private static async Task<Results<Ok<AccessRequestResponse>, NotFound, BadRequest<IReadOnlyList<string>>, Conflict<string>>> UpdateAccessRequestStatus(
        Guid id,
        UpdateAccessRequestStatusRequest request,
        ClaimsPrincipal user,
        IAuthService authService,
        CancellationToken ct
    )
    {
        var result = await authService.UpdateAccessRequestStatusAsync(id, request, user, ct);

        return result.Match<Results<Ok<AccessRequestResponse>, NotFound, BadRequest<IReadOnlyList<string>>, Conflict<string>>>(
            response => TypedResults.Ok(response),
            _ => TypedResults.NotFound(),
            _ => TypedResults.Conflict("This access request has already been processed"),
            validationFailed => TypedResults.BadRequest(validationFailed.Errors)
        );
    }
}
