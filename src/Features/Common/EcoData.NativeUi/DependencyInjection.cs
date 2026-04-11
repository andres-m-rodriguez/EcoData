using EcoData.NativeUi.Components.NativeNavbar;
using EcoData.NativeUi.Components.NativeNavigation;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.NativeUi;

public static class DependencyInjection
{
    /// <summary>
    /// Adds NativeUI services to the service collection.
    /// </summary>
    public static IServiceCollection AddNativeUi(this IServiceCollection services)
    {
        services.AddScoped<INativeNavbarManager, NativeNavbarManager>();
        services.AddScoped<INativeNavigationManager, NativeNavigationManager>();
        return services;
    }
}
