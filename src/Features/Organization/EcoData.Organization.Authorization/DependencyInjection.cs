using EcoData.Common.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Organization.Authorization;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the source answering organization-scoped permission and role questions.
    /// Requires <c>AddOrganizationDataAccess</c> for the repositories it reads.
    /// </summary>
    public static IServiceCollection AddOrganizationPermissionSource(
        this IServiceCollection services
    )
    {
        services.AddHttpContextAccessor();
        services.AddPermissionSource<OrganizationPermissionSource>();

        return services;
    }
}
