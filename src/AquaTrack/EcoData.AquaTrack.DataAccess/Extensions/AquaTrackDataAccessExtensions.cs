using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.AquaTrack.DataAccess.Repositories;
using EcoData.AquaTrack.DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.AquaTrack.DataAccess.Extensions;

public static class AquaTrackDataAccessExtensions
{
    public static IServiceCollection AddAquaTrackDataAccess(this IServiceCollection services)
    {
        services.AddScoped<IDataSourceRepository, DataSourceRepository>();
        services.AddScoped<ISensorRepository, SensorRepository>();
        services.AddScoped<IReadingRepository, ReadingRepository>();
        services.AddScoped<IIngestionLogRepository, IngestionLogRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ISensorHealthRepository, SensorHealthRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<ISensorTypeRepository, SensorTypeRepository>();
        services.AddScoped<IParameterRepository, ParameterRepository>();
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
        services.AddScoped<IOrganizationMembershipRepository, OrganizationMembershipRepository>();
        services.AddScoped<
            IOrganizationAccessRequestRepository,
            OrganizationAccessRequestRepository
        >();
        services.AddScoped<IOrganizationBlockedUserRepository, OrganizationBlockedUserRepository>();
        services.AddScoped<IPermissionService, PermissionService>();

        return services;
    }
}
