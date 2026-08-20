using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Wildlife.Application;

public static class DependencyInjection
{
    // The host must also call AddPermissions and register a source for the
    // organization scope kind.
    public static IServiceCollection AddWildlifeAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IWildlifePermission, WildlifePermission>();

        return services;
    }
}
