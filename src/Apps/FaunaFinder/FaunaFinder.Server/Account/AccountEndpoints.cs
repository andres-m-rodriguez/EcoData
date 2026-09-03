using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.Contracts.Requests;
using FaunaFinder.Client.Services.Account;
using FaunaFinder.Server.Authentication;
using FaunaFinder.Server.Organization;
using Microsoft.AspNetCore.Http.HttpResults;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace FaunaFinder.Server.Account;

// FaunaFinder holds no identity data and no JWT secrets. Every account
// operation is proxied to EcoPortal, and the returned token is re-issued as
// this origin's own auth cookie because EcoPortal's cookie never reaches the
// faunafinder hostname.
public static class AccountEndpoints
{
    public const string HttpClientName = "ecoportal-auth";
    private const string AuthCookieName = EcoPortalSessionAuthenticationHandler.CookieName;

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/account").WithTags("Account");

        group
            .MapPost(
                "/signup",
                async Task<Results<Ok<FaunaFinderSignupResponse>, ContentHttpResult, ProblemHttpResult>> (
                    FaunaFinderSignupRequest request,
                    IHttpClientFactory httpClientFactory,
                    FaunaFinderOrganizationLoader organizationLoader,
                    ILoggerFactory loggerFactory,
                    HttpContext httpContext,
                    CancellationToken ct
                ) =>
                {
                    var httpClient = httpClientFactory.CreateClient(HttpClientName);

                    try
                    {
                        var registerResponse = await httpClient.PostAsJsonAsync(
                            "identity/auth/register",
                            new RegisterRequest(
                                request.Email,
                                request.DisplayName,
                                request.Password,
                                request.ConfirmPassword
                            ),
                            ct
                        );

                        if (!registerResponse.IsSuccessStatusCode)
                            return await RelayAsync(registerResponse, ct);

                        var loginResponse = await httpClient.PostAsJsonAsync(
                            "identity/auth/login",
                            new LoginRequest(request.Email, request.Password),
                            ct
                        );

                        if (!loginResponse.IsSuccessStatusCode)
                            return await RelayAsync(loginResponse, ct);

                        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(ct))!;

                        // The access request is best-effort: the account already exists
                        // and the user can ask again later, so a failure here must not
                        // fail the signup. The response flag lets the UI say so.
                        var accessRequestSubmitted = false;
                        if (request.IsStudent && organizationLoader.Current is { } organization)
                        {
                            try
                            {
                                using var accessRequest = new HttpRequestMessage(
                                    HttpMethod.Post,
                                    $"organization/organizations/{organization.Id}/access-requests"
                                )
                                {
                                    Content = JsonContent.Create(
                                        new CreateOrganizationAccessRequestRequest(
                                            organization.Id,
                                            FaunaFinderRoles.Student
                                        )
                                    ),
                                };
                                accessRequest.Headers.Add(
                                    "Cookie",
                                    $"{AuthCookieName}={login.Token}"
                                );

                                var accessResponse = await httpClient.SendAsync(accessRequest, ct);
                                accessRequestSubmitted = accessResponse.IsSuccessStatusCode;

                                if (!accessRequestSubmitted)
                                    loggerFactory
                                        .CreateLogger(nameof(AccountEndpoints))
                                        .LogWarning(
                                            "Student access request for {UserId} returned {StatusCode}",
                                            login.User.Id,
                                            (int)accessResponse.StatusCode
                                        );
                            }
                            catch (Exception e)
                                when (e is HttpRequestException
                                        or TimeoutRejectedException
                                        or BrokenCircuitException
                                )
                            {
                                loggerFactory
                                    .CreateLogger(nameof(AccountEndpoints))
                                    .LogWarning(
                                        e,
                                        "EcoPortal unreachable while submitting the student access request for {UserId}",
                                        login.User.Id
                                    );
                            }
                        }

                        AppendAuthCookie(httpContext, login);
                        return TypedResults.Ok(
                            new FaunaFinderSignupResponse(login.User, accessRequestSubmitted)
                        );
                    }
                    catch (Exception e)
                        when (e is HttpRequestException
                                or TimeoutRejectedException
                                or BrokenCircuitException
                        )
                    {
                        return TypedResults.Problem(
                            detail: "Sign-up is temporarily unavailable. Please try again later.",
                            statusCode: StatusCodes.Status503ServiceUnavailable
                        );
                    }
                }
            )
            .WithName("Signup");

        group
            .MapPost(
                "/login",
                async Task<Results<Ok<UserInfo>, ContentHttpResult, ProblemHttpResult>> (
                    LoginRequest request,
                    IHttpClientFactory httpClientFactory,
                    HttpContext httpContext,
                    CancellationToken ct
                ) =>
                {
                    var httpClient = httpClientFactory.CreateClient(HttpClientName);

                    try
                    {
                        var response = await httpClient.PostAsJsonAsync(
                            "identity/auth/login",
                            request,
                            ct
                        );

                        if (!response.IsSuccessStatusCode)
                            return await RelayAsync(response, ct);

                        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>(ct))!;

                        AppendAuthCookie(httpContext, login);
                        return TypedResults.Ok(login.User);
                    }
                    catch (Exception e)
                        when (e is HttpRequestException
                                or TimeoutRejectedException
                                or BrokenCircuitException
                        )
                    {
                        return TypedResults.Problem(
                            detail: "Sign-in is temporarily unavailable. Please try again later.",
                            statusCode: StatusCodes.Status503ServiceUnavailable
                        );
                    }
                }
            )
            .WithName("Login");

        group
            .MapPost(
                "/logout",
                (HttpContext httpContext) =>
                {
                    httpContext.Response.Cookies.Delete(AuthCookieName);
                    return TypedResults.Ok();
                }
            )
            .WithName("Logout");

        group
            .MapGet(
                "/me",
                async Task<Results<Ok<UserInfo>, UnauthorizedHttpResult, ProblemHttpResult>> (
                    IHttpClientFactory httpClientFactory,
                    HttpContext httpContext,
                    CancellationToken ct
                ) =>
                {
                    if (!httpContext.Request.Cookies.TryGetValue(AuthCookieName, out var token))
                        return TypedResults.Unauthorized();

                    var httpClient = httpClientFactory.CreateClient(HttpClientName);

                    try
                    {
                        using var request = new HttpRequestMessage(
                            HttpMethod.Get,
                            "identity/auth/me"
                        );
                        request.Headers.Add("Cookie", $"{AuthCookieName}={token}");

                        var response = await httpClient.SendAsync(request, ct);
                        if (!response.IsSuccessStatusCode)
                            return TypedResults.Unauthorized();

                        var userInfo = (await response.Content.ReadFromJsonAsync<UserInfo>(ct))!;
                        return TypedResults.Ok(userInfo);
                    }
                    catch (Exception e)
                        when (e is HttpRequestException
                                or TimeoutRejectedException
                                or BrokenCircuitException
                        )
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable
                        );
                    }
                }
            )
            .WithName("GetCurrentUser");

        group
            .MapGet(
                "/access-requests",
                async Task<
                    Results<Ok<List<OrganizationAccessRequestDto>>, UnauthorizedHttpResult, ProblemHttpResult>
                > (
                    IHttpClientFactory httpClientFactory,
                    FaunaFinderOrganizationLoader organizationLoader,
                    HttpContext httpContext,
                    CancellationToken ct
                ) =>
                {
                    if (!httpContext.Request.Cookies.TryGetValue(AuthCookieName, out var token))
                        return TypedResults.Unauthorized();

                    if (organizationLoader.Current is not { } organization)
                        return TypedResults.Ok(new List<OrganizationAccessRequestDto>());

                    var httpClient = httpClientFactory.CreateClient(HttpClientName);

                    try
                    {
                        using var request = new HttpRequestMessage(
                            HttpMethod.Get,
                            "organization/me/access-requests"
                        );
                        request.Headers.Add("Cookie", $"{AuthCookieName}={token}");

                        var response = await httpClient.SendAsync(request, ct);
                        if (!response.IsSuccessStatusCode)
                            return TypedResults.Unauthorized();

                        var requests = (
                            await response.Content.ReadFromJsonAsync<
                                List<OrganizationAccessRequestDto>
                            >(ct)
                        )!;
                        var mine = requests.Where(r => r.OrganizationId == organization.Id).ToList();
                        return TypedResults.Ok(mine);
                    }
                    catch (Exception e)
                        when (e is HttpRequestException
                                or TimeoutRejectedException
                                or BrokenCircuitException
                        )
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable
                        );
                    }
                }
            )
            .WithName("GetMyAccessRequests");

        group
            .MapGet(
                "/organization",
                Results<Ok<FaunaFinderOrganizationDto>, ProblemHttpResult> (
                    FaunaFinderOrganizationLoader organizationLoader
                ) =>
                {
                    if (organizationLoader.Current is not { } organization)
                        return TypedResults.Problem(
                            detail: "The FaunaFinder organization has not been resolved yet.",
                            statusCode: StatusCodes.Status503ServiceUnavailable
                        );

                    return TypedResults.Ok(
                        new FaunaFinderOrganizationDto(
                            organization.Id,
                            organization.Name,
                            organization.Slug
                        )
                    );
                }
            )
            .WithName("GetOrganization");

        group
            .MapGet(
                "/permissions",
                async Task<Results<Ok<UserPermissionsDto>, ContentHttpResult, ProblemHttpResult>> (
                    IHttpClientFactory httpClientFactory,
                    FaunaFinderOrganizationLoader organizationLoader,
                    HttpContext httpContext,
                    CancellationToken ct
                ) =>
                {
                    if (organizationLoader.Current is not { } organization)
                        return TypedResults.Problem(
                            detail: "The FaunaFinder organization has not been resolved yet.",
                            statusCode: StatusCodes.Status503ServiceUnavailable
                        );

                    // The authorization policy already required the cookie.
                    var token = httpContext.Request.Cookies[AuthCookieName];
                    var httpClient = httpClientFactory.CreateClient(HttpClientName);

                    try
                    {
                        using var request = new HttpRequestMessage(
                            HttpMethod.Get,
                            $"organization/organizations/{organization.Id}/my-permissions"
                        );
                        request.Headers.Add("Cookie", $"{AuthCookieName}={token}");

                        var response = await httpClient.SendAsync(request, ct);
                        if (!response.IsSuccessStatusCode)
                            return await RelayAsync(response, ct);

                        var permissions = (
                            await response.Content.ReadFromJsonAsync<UserPermissionsDto>(ct)
                        )!;
                        return TypedResults.Ok(permissions);
                    }
                    catch (Exception e)
                        when (e is HttpRequestException
                                or TimeoutRejectedException
                                or BrokenCircuitException
                        )
                    {
                        return TypedResults.Problem(
                            statusCode: StatusCodes.Status503ServiceUnavailable
                        );
                    }
                }
            )
            .RequireAuthorization()
            .WithName("GetMyPermissions");

        return app;
    }

    private static void AppendAuthCookie(HttpContext httpContext, LoginResponse login) =>
        httpContext.Response.Cookies.Append(
            AuthCookieName,
            login.Token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = login.ExpiresAt,
            }
        );

    private static async Task<ContentHttpResult> RelayAsync(
        HttpResponseMessage response,
        CancellationToken ct
    )
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        return TypedResults.Text(
            body,
            response.Content.Headers.ContentType?.ToString() ?? "application/problem+json",
            statusCode: (int)response.StatusCode
        );
    }
}
