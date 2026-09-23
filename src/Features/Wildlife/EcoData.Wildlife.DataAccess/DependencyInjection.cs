using EcoData.Wildlife.DataAccess.Interfaces;
using EcoData.Wildlife.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Wildlife.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddWildlifeDataAccess(this IServiceCollection services)
    {
        // The stats decorator needs a cache; AddMemoryCache is a TryAdd, so a host
        // that already registered one keeps its own.
        services.AddMemoryCache();
        services.AddScoped<SpeciesRepository>();
        services.AddScoped<ISpeciesRepository, CachedSpeciesRepository>();
        services.AddScoped<ISpeciesCategoryRepository, SpeciesCategoryRepository>();
        services.AddScoped<INrcsPracticeRepository, NrcsPracticeRepository>();
        services.AddScoped<IFwsActionRepository, FwsActionRepository>();
        services.AddScoped<IConservationLinkRepository, ConservationLinkRepository>();

        return services;
    }
}
