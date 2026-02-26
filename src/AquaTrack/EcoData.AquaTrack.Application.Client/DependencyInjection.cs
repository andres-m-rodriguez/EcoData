using Microsoft.Extensions.DependencyInjection;

namespace EcoData.AquaTrack.Application.Client;

public static class DependencyInjection
{
    public static IServiceCollection AddAquaTrackClient(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null
    )
    {
        services.AddHttpClient<ISensorHttpClient, SensorHttpClient>(client =>
        {
            configureClient?.Invoke(client);
        });

        return services;
    }

    public static IServiceCollection AddAquaTrackClient(
        this IServiceCollection services,
        Uri baseAddress
    )
    {
        return services.AddAquaTrackClient(client => client.BaseAddress = baseAddress);
    }
}
