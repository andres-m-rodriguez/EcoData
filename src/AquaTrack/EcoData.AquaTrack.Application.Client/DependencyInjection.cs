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

        services.AddHttpClient<IDataSourceHttpClient, DataSourceHttpClient>(client =>
        {
            configureClient?.Invoke(client);
        });

        services.AddHttpClient<IOrganizationHttpClient, OrganizationHttpClient>(client =>
        {
            configureClient?.Invoke(client);
        });

        services.AddHttpClient<IOrganizationMemberHttpClient, OrganizationMemberHttpClient>(
            client =>
            {
                configureClient?.Invoke(client);
            }
        );

        services.AddHttpClient<ILocationHttpClient, LocationHttpClient>(client =>
        {
            configureClient?.Invoke(client);
        });

        services.AddHttpClient<IPermissionHttpClient, PermissionHttpClient>(client =>
        {
            configureClient?.Invoke(client);
        });

        services.AddHttpClient<
            IOrganizationAccessRequestHttpClient,
            OrganizationAccessRequestHttpClient
        >(client =>
        {
            configureClient?.Invoke(client);
        });

        services.AddHttpClient<
            IOrganizationBlockedUserHttpClient,
            OrganizationBlockedUserHttpClient
        >(client =>
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
