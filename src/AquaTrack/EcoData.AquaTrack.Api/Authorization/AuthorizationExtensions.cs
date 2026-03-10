using System.Security.Claims;
using EcoData.AquaTrack.Api.Authentication;
using EcoData.Identity.Contracts.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.AquaTrack.Api.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAquaTrackAuthorization(this IServiceCollection services)
    {
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
