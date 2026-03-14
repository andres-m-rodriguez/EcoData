using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Sensors.Application.Client;

public static class DependencyInjection
{
    public static IServiceCollection AddSensorsClient(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null
    )
    {
        services.AddHttpClient<ISensorHttpClient, SensorHttpClient>(client =>
        {
            configureClient?.Invoke(client);
        });

        services.AddHttpClient<ISensorReadingHttpClient, SensorReadingHttpClient>(client =>
        {
            configureClient?.Invoke(client);
        });

        services.AddHttpClient<ISensorHealthHttpClient, SensorHealthHttpClient>(client =>
        {
            configureClient?.Invoke(client);
        });

        return services;
    }

    public static IServiceCollection AddSensorsClient(
        this IServiceCollection services,
        Uri baseAddress
    )
    {
        return services.AddSensorsClient(client => client.BaseAddress = baseAddress);
    }
}
