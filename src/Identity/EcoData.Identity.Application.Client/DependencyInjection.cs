using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Identity.Application.Client;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityClient(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null
    )
    {
        services.AddHttpClient<IAuthHttpClient, AuthHttpClient>(client =>
        {
            configureClient?.Invoke(client);
        });

        return services;
    }

    public static IServiceCollection AddIdentityClient(
        this IServiceCollection services,
        Uri baseAddress
    )
    {
        return services.AddIdentityClient(client => client.BaseAddress = baseAddress);
    }
}
