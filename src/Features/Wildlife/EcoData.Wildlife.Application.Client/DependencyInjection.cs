using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Wildlife.Application.Client;

public static class DependencyInjection
{
    public static IServiceCollection AddWildlifeClient(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient<ISpeciesHttpClient, SpeciesHttpClient>(client =>
        {
            client.BaseAddress = baseAddress;
        });

        services.AddHttpClient<ISpeciesCategoryHttpClient, SpeciesCategoryHttpClient>(client =>
        {
            client.BaseAddress = baseAddress;
        });

        services.AddHttpClient<IFwsActionHttpClient, FwsActionHttpClient>(client =>
        {
            client.BaseAddress = baseAddress;
        });

        services.AddHttpClient<INrcsPracticeHttpClient, NrcsPracticeHttpClient>(client =>
        {
            client.BaseAddress = baseAddress;
        });

        services.AddHttpClient<IConservationLinkHttpClient, ConservationLinkHttpClient>(client =>
        {
            client.BaseAddress = baseAddress;
        });

        return services;
    }
}
