using Microsoft.Extensions.DependencyInjection;
using Tempest;

namespace EcoData.Spa.Core;

public static class DependencyInjection
{
    /// <summary>
    /// Adds the SPA shared kernel: the Tempest event bus that every SPA feature
    /// library publishes its state changes on.
    ///
    /// <para>Feature libraries call this from their own registration extension,
    /// so a host normally reaches it through <c>AddSpaNavigation()</c> rather
    /// than directly. Calling it alongside <c>AddTempest()</c>, in either order
    /// and any number of times, still registers the bus once.</para>
    /// </summary>
    public static IServiceCollection AddSpaCore(this IServiceCollection services)
    {
        if (services.All(descriptor => descriptor.ServiceType != typeof(IEventBus)))
            services.AddTempest();

        return services;
    }
}
