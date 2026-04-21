using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Wildlife.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddWildlifeDataAccess(this IServiceCollection services)
    {
        // Repositories will be registered here as they are implemented
        // Example:
        // services.AddScoped<ISpeciesRepository, SpeciesRepository>();
        // services.AddScoped<ISightingRepository, SightingRepository>();

        return services;
    }
}
