using System.Security.Claims;
using EcoData.AquaTrack.Api.Authentication;
using EcoData.Identity.Contracts.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.AquaTrack.Api.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAquaTrackAuthorization(this IServiceCollection services)
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

            options.AddPolicy(
                PolicyNames.ApiKey,
                policy =>
                    policy
                        .AddAuthenticationSchemes(ApiKeyAuthenticationOptions.DefaultScheme)
                        .RequireAuthenticatedUser()
            );
        });

        return services;
    }
}
