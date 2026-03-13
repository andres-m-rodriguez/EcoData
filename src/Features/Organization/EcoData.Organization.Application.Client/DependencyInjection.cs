using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Organization.Application.Client;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationClient(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null
    )
    {
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

        services.AddHttpClient<IOrganizationAccessRequestHttpClient, OrganizationAccessRequestHttpClient>(client =>
        {
            configureClient?.Invoke(client);
        });

        services.AddHttpClient<IOrganizationBlockedUserHttpClient, OrganizationBlockedUserHttpClient>(client =>
        {
            configureClient?.Invoke(client);
        });

        services.AddHttpClient<IPermissionHttpClient, PermissionHttpClient>(client =>
        {
            configureClient?.Invoke(client);
        });

        return services;
    }

    public static IServiceCollection AddOrganizationClient(
        this IServiceCollection services,
        Uri baseAddress
    )
    {
        return services.AddOrganizationClient(client => client.BaseAddress = baseAddress);
    }
}
