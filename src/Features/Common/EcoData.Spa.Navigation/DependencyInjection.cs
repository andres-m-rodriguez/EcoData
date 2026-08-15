using EcoData.Spa.Core;
using EcoData.Spa.Navigation.Navbar;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Spa.Navigation;

public static class DependencyInjection
{
    /// <summary>
    /// Adds the navigation and navbar managers, and the shared kernel they
    /// publish on. A host calls this instead of <c>AddSpaCore()</c> directly.
    /// </summary>
    public static IServiceCollection AddSpaNavigation(this IServiceCollection services)
    {
        services.AddSpaCore();
        services.AddScoped<INavigationManager, SpaNavigationManager>();
        services.AddScoped<INavbarManager, SpaNavbarManager>();
        return services;
    }
}
