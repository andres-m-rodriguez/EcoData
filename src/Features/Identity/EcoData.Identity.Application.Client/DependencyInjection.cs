using System.Security.Claims;
using EcoData.Identity.Application.Client.HttpClients;
using EcoData.Identity.Contracts.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Identity.Application.Client;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityClient(
        this IServiceCollection services,
        Action<HttpClient>? configureClient = null
    )
    {
        services.AddHttpClient<IAuthHttpClient, AuthHttpClient>()
            .ConfigureHttpClient(client => configureClient?.Invoke(client));

        return services;
    }

    public static IServiceCollection AddIdentityClient(
        this IServiceCollection services,
        Uri baseAddress
    )
    {
        return services.AddIdentityClient(client => client.BaseAddress = baseAddress);
    }

    public static IServiceCollection AddIdentityAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(
                PolicyNames.Admin,
                policy => policy.RequireClaim(ClaimTypes.Role, "Admin")
            );
        });

        return services;
    }
}
