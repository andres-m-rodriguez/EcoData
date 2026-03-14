using EcoData.Identity.Application.Server.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Identity.Application.Server;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplicationServer<TUserLookupService>(
        this IServiceCollection services
    )
        where TUserLookupService : class, IUserLookupService
    {
        services.AddScoped<IUserLookupService, TUserLookupService>();
        return services;
    }
}
