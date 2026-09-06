using EcoData.Spa.Interop;
using EcoData.Spa.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Spa;

public static class DependencyInjection
{
    // Hosts register Tempest themselves; the navigation services publish on its bus.
    public static IServiceCollection AddEcoDataSpa(this IServiceCollection services)
    {
        // Stateless over IJSRuntime, so it takes the lifetime of whatever consumes it.
        services.AddTransient<IJavascriptSafeInterop, JavascriptSafeInterop>();
        services.AddScoped<PageNavigation>();
        services.AddScoped<Navbar>();
        return services;
    }
}
