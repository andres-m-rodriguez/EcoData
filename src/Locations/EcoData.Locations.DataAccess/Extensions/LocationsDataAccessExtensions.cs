using EcoData.Locations.DataAccess.Interfaces;
using EcoData.Locations.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Locations.DataAccess.Extensions;

public static class LocationsDataAccessExtensions
{
    public static IServiceCollection AddLocationsDataAccess(this IServiceCollection services)
    {
        services.AddScoped<IStateRepository, StateRepository>();
        services.AddScoped<IMunicipalityRepository, MunicipalityRepository>();

        return services;
    }
}
