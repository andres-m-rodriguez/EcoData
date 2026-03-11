using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EcoData.AquaTrack.Api.Authorization;

public sealed class OrganizationPermissionPolicyProvider(
    IOptions<AuthorizationOptions> options
) : IAuthorizationPolicyProvider
{
    public const string PolicyPrefix = "OrganizationPermission:";

    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix))
        {
            var permission = policyName[PolicyPrefix.Length..];

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new OrganizationPermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();
}
