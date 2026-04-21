using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Wildlife.Application.Server;

public static class DependencyInjection
{
    public static IServiceCollection AddWildlifeApplication(this IServiceCollection services)
    {
        // Services will be registered here as they are implemented
        // Example:
        // services.AddScoped<ISpeciesService, SpeciesService>();
        // services.AddScoped<ISightingService, SightingService>();

        return services;
    }
}
