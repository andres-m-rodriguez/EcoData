using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using EcoData.Identity.Contracts.Claims;
using EcoData.Identity.Contracts.Responses;
using FaunaFinder.Server.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace FaunaFinder.Server.Authentication;

// FaunaFinder holds no JWT secrets, so the session cookie is validated by
// asking EcoPortal who it belongs to, the same way /account/me does. The
// answer is cached briefly so one page load does not fan out into one
// upstream call per API request; logout deletes the cookie, which makes a
// stale entry unreachable.
public sealed class EcoPortalSessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "EcoPortalSession";
    public const string CookieName = "auth_token";

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Request.Cookies[CookieName];

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        var cacheKey =
            "ecoportal-session:"
            + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        if (cache.TryGetValue(cacheKey, out UserInfo? cached) && cached is not null)
        {
            return AuthenticateResult.Success(
                new AuthenticationTicket(cached.ToClaimsPrincipal(SchemeName), SchemeName)
            );
        }

        var httpClient = httpClientFactory.CreateClient(AccountEndpoints.HttpClientName);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "identity/auth/me");
            request.Headers.Add("Cookie", $"{CookieName}={token}");

            var response = await httpClient.SendAsync(request, Context.RequestAborted);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return AuthenticateResult.Fail("EcoPortal rejected the session token");
            }

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning(
                    "EcoPortal answered {StatusCode} while validating a session",
                    (int)response.StatusCode
                );
                return AuthenticateResult.Fail("EcoPortal could not validate the session");
            }

            var user = (
                await response.Content.ReadFromJsonAsync<UserInfo>(Context.RequestAborted)
            )!;
            cache.Set(cacheKey, user, CacheDuration);

            return AuthenticateResult.Success(
                new AuthenticationTicket(user.ToClaimsPrincipal(SchemeName), SchemeName)
            );
        }
        catch (Exception e)
            when (e is HttpRequestException or TimeoutRejectedException or BrokenCircuitException)
        {
            Logger.LogWarning(e, "EcoPortal unreachable while validating a session");
            return AuthenticateResult.Fail("EcoPortal is unreachable");
        }
    }
}
