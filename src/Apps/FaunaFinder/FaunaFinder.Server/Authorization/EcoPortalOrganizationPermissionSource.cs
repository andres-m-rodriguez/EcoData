using System.Net;
using EcoData.Common.Authorization;
using EcoData.Organization.Contracts.Dtos;
using FaunaFinder.Server.Account;
using FaunaFinder.Server.Authentication;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace FaunaFinder.Server.Authorization;

// Organization membership lives in EcoPortal, so grants are fetched through
// my-permissions with the caller's cookie forwarded. Organization's own
// OrganizationPermissionHttpSource cannot be reused: it relies on the
// browser's cookie through a typed client. Scoped, so one fetch per
// organization per request.
public sealed class EcoPortalOrganizationPermissionSource(
    IHttpContextAccessor httpContextAccessor,
    IHttpClientFactory httpClientFactory,
    ILogger<EcoPortalOrganizationPermissionSource> logger
) : IOrganizationPermissionSource
{
    private readonly Dictionary<Guid, Task<UserPermissionsDto>> _cache = [];

    public async Task<bool> HasAsync(
        IOrganizationPermission permission,
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        var permissions = await GetPermissionsAsync(organizationId, cancellationToken);

        if (permissions.IsGlobalAdmin)
            return true;

        return permissions.Permissions.Contains(permission.Key);
    }

    public async Task<OrganizationGrants> GrantsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        var permissions = await GetPermissionsAsync(organizationId, cancellationToken);

        return new OrganizationGrants(
            permissions.Permissions.ToHashSet(StringComparer.Ordinal),
            permissions.IsGlobalAdmin
        );
    }

    private Task<UserPermissionsDto> GetPermissionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken
    )
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (
            httpContext is null
            || !httpContext.Request.Cookies.TryGetValue(
                EcoPortalSessionAuthenticationHandler.CookieName,
                out var token
            )
            || string.IsNullOrEmpty(token)
        )
            return Task.FromResult(
                new UserPermissionsDto(organizationId, [], IsGlobalAdmin: false)
            );

        if (!_cache.TryGetValue(organizationId, out var task))
        {
            task = FetchAsync(organizationId, token, cancellationToken);
            _cache[organizationId] = task;
        }

        return task;
    }

    // An answer EcoPortal cannot give reads as "no grants": an endpoint check
    // denies rather than throws, and the warning is what tells an upstream
    // failure apart from a correct denial.
    private async Task<UserPermissionsDto> FetchAsync(
        Guid organizationId,
        string token,
        CancellationToken cancellationToken
    )
    {
        var httpClient = httpClientFactory.CreateClient(AccountEndpoints.HttpClientName);

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"organization/organizations/{organizationId}/my-permissions"
            );
            request.Headers.Add(
                "Cookie",
                $"{EcoPortalSessionAuthenticationHandler.CookieName}={token}"
            );

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                    logger.LogWarning(
                        "EcoPortal answered {StatusCode} while resolving permissions for organization {OrganizationId}",
                        (int)response.StatusCode,
                        organizationId
                    );

                return new UserPermissionsDto(organizationId, [], IsGlobalAdmin: false);
            }

            return (
                await response.Content.ReadFromJsonAsync<UserPermissionsDto>(cancellationToken)
            )!;
        }
        catch (Exception e)
            when (e is HttpRequestException or TimeoutRejectedException or BrokenCircuitException)
        {
            logger.LogWarning(
                e,
                "EcoPortal unreachable while resolving permissions for organization {OrganizationId}",
                organizationId
            );
            return new UserPermissionsDto(organizationId, [], IsGlobalAdmin: false);
        }
    }
}
