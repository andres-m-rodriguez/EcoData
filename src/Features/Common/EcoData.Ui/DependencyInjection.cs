using EcoData.Ui.Interop;
using EcoData.Ui.Shell.Navbar;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Ui;

public static class DependencyInjection
{
    public static IServiceCollection AddEcoDataUi(this IServiceCollection services)
    {
        services.AddScoped<IJavascriptSafeInterop, JavascriptSafeInterop>();

        // One watcher per bar that renders it, so it is transient.
        services.AddTransient<NavAutoHide>();

        return services;
    }
}
