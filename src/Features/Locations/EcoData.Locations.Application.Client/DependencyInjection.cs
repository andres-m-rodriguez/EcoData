using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Locations.Application.Client;

public static class DependencyInjection
{
    public static IServiceCollection AddLocationsClient(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null)
    {
        services.AddHttpClient<IMunicipalityHttpClient, MunicipalityHttpClient>(client =>
        {
            configureClient?.Invoke(client);
        });

        return services;
    }

    public static IServiceCollection AddLocationsClient(
        this IServiceCollection services,
        Uri baseAddress)
    {
        return services.AddLocationsClient(client => client.BaseAddress = baseAddress);
    }
}
