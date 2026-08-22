using EcoData.Common.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Organization.Authorization;

public static class DependencyInjection
{
    // Requires the Organization application services for IOrganizationPermissionService.
    public static IServiceCollection AddOrganizationPermissionSource(
        this IServiceCollection services
    )
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IOrganizationPermissionSource, OrganizationPermissionSource>();

        return services;
    }
}
