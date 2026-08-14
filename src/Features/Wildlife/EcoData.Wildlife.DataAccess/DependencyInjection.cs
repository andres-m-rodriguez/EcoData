using EcoData.Wildlife.Contracts;
using EcoData.Wildlife.DataAccess.Interfaces;
using EcoData.Wildlife.DataAccess.Repositories;
using EcoData.Wildlife.DataAccess.Storage;
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
        services.AddScoped<INrcsPracticeRepository, NrcsPracticeRepository>();
        services.AddScoped<IFwsActionRepository, FwsActionRepository>();
        services.AddScoped<IConservationLinkRepository, ConservationLinkRepository>();

        services.AddWildlifeImageStorage();

        return services;
    }

    /// <summary>
    /// Blob-backed species image storage on its own, for hosts that write images
    /// without serving the wildlife API — the seeder. Expects the host to have
    /// registered a <c>BlobServiceClient</c> (<c>AddAzureBlobServiceClient</c>).
    /// </summary>
    public static IServiceCollection AddWildlifeImageStorage(this IServiceCollection services)
    {
        services.AddSingleton<ISpeciesImageStore, SpeciesImageStore>();

        return services;
    }
}
