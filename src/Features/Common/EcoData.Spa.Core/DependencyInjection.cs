using EcoData.Spa.Core.Navbar;
using EcoData.Spa.Core.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Tempest;

namespace EcoData.Spa.Core;

public static class DependencyInjection
{
    /// <summary>
    /// Adds the SPA shell services — navigation and navbar — to the service
    /// collection.
    ///
    /// <para>Both publish their state changes on Tempest's <see cref="IEventBus"/>,
    /// so the bus is registered here when the host has not already called
    /// <c>AddTempest()</c>. Calling both, in either order, registers it once.</para>
    /// </summary>
    public static IServiceCollection AddSpaCore(this IServiceCollection services)
    {
        if (services.All(descriptor => descriptor.ServiceType != typeof(IEventBus)))
        {
            services.AddTempest();
        }

        services.AddScoped<INavigationManager, SpaNavigationManager>();
        services.AddScoped<INavbarManager, SpaNavbarManager>();
        return services;
    }
}
