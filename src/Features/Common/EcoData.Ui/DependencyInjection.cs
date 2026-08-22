using EcoData.Ui.Interop;
using EcoData.Ui.Shell.Navbar;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Ui;

public static class DependencyInjection
{
    public static IServiceCollection AddEcoDataUi(this IServiceCollection services)
    {
        // Stateless over IJSRuntime, so it takes the lifetime of whatever consumes
        // it — a singleton service as readily as a component.
        services.AddTransient<IJavascriptSafeInterop, JavascriptSafeInterop>();

        // One watcher per bar that renders it, so it is transient.
        services.AddTransient<NavAutoHide>();

        return services;
    }
}
