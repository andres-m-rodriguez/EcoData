using EcoData.Common.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Organization.Authorization;

public static class DependencyInjection
{
    // Requires AddOrganizationDataAccess for the repositories this reads.
    public static IServiceCollection AddOrganizationPermissionSource(
        this IServiceCollection services
    )
    {
        services.AddHttpContextAccessor();
        services.AddPermissionSource<OrganizationPermissionSource>();

        return services;
    }
}
