using EcoData.Wildlife.Contracts;
using EcoData.Wildlife.DataAccess.Interfaces;
using EcoData.Wildlife.DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Wildlife.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddWildlifeDataAccess(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<WildlifeOptions>()
            .Bind(configuration.GetSection(WildlifeOptions.SectionName));

        services.AddScoped<ISpeciesRepository, SpeciesRepository>();
        services.AddScoped<ISpeciesCategoryRepository, SpeciesCategoryRepository>();
        services.AddScoped<IConservationRepository, ConservationRepository>();

        return services;
    }
}
