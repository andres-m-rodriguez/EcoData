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
    /// <param name="rootPaths">
    /// The app's navigation roots — the destinations its tab bar owns. Landing
    /// on one resets the back stack; every other page can be gone back from.
    /// Pass none to keep the old guess that any single-segment path is a root,
    /// which is wrong for any app whose sections link sideways.
    /// </param>
    public static IServiceCollection AddSpaNavigation(
        this IServiceCollection services,
        params string[] rootPaths)
    {
        services.AddSpaCore();
        services.AddSingleton(new SpaNavigationOptions { RootPaths = rootPaths });
        services.AddScoped<INavigationManager, SpaNavigationManager>();
        services.AddScoped<INavbarManager, SpaNavbarManager>();
        return services;
    }
}
