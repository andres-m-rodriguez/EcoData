using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Wildlife.Application.Server;

public static class DependencyInjection
{
    public static IServiceCollection AddWildlifeApplication(this IServiceCollection services)
    {
        return services;
    }
}
