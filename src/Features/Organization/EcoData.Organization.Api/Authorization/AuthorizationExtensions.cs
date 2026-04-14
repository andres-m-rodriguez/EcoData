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
                policy => policy.RequireClaim(ClaimTypes.Role, "Admin")
            );
        });

        return services;
    }
}
