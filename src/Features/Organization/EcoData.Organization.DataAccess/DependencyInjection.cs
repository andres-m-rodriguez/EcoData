using EcoData.Organization.Application.Server.Services;
using EcoData.Organization.DataAccess.Interfaces;
using EcoData.Organization.DataAccess.Repositories;
using EcoData.Organization.DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Organization.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationDataAccess(this IServiceCollection services)
    {
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
        services.AddScoped<IOrganizationMembershipRepository, OrganizationMembershipRepository>();
        services.AddScoped<IOrganizationAccessRequestRepository, OrganizationAccessRequestRepository>();
        services.AddScoped<IOrganizationBlockedUserRepository, OrganizationBlockedUserRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IDataSourceRepository, DataSourceRepository>();
        services.AddScoped<IOrganizationPermissionService, OrganizationPermissionService>();

        return services;
    }
}
