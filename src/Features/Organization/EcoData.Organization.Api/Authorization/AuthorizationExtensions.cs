using System.Security.Claims;
using EcoData.Identity.Contracts.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Organization.Api.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddOrganizationAuthorization(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IAuthorizationPolicyProvider, OrganizationPermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, OrganizationPermissionHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                PolicyNames.Admin,
                // The role claim carries the GlobalRole enum name: the JWT and the client principal both
                // emit it that way, so the policy matches that value, not its own name.
                policy => policy.RequireClaim(ClaimTypes.Role, nameof(GlobalRole.GlobalAdmin))
            );
        });

        return services;
    }
}
