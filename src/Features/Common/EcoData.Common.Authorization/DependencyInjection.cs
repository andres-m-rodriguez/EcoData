using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Common.Authorization;

public static class DependencyInjection
{
    // Named to avoid colliding with ASP.NET Core's AddAuthorization, which hosts also call.
    // Modules that own a scope's storage register their typed source interface themselves.
    public static IServiceCollection AddPermissions(this IServiceCollection services)
    {
        services.AddScoped<IAuthorization, Authorization>();

        return services;
    }
}
