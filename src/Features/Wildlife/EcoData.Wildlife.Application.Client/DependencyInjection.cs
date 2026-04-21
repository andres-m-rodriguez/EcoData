using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Wildlife.Application.Client;

public static class DependencyInjection
{
    public static IServiceCollection AddWildlifeApplicationClient(this IServiceCollection services)
    {
        // HTTP clients will be registered here as they are implemented
        // Example:
        // services.AddHttpClient<ISpeciesHttpClient, SpeciesHttpClient>();

        return services;
    }
}
