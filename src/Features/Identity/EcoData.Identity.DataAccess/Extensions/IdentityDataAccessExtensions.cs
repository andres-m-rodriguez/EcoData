using EcoData.Identity.Application.Server.Services;
using EcoData.Identity.DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Identity.DataAccess.Extensions;

public static class IdentityDataAccessExtensions
{
    public static IServiceCollection AddIdentityDataAccess(this IServiceCollection services)
    {
        services.AddScoped<IUserLookupService, UserLookupService>();

        return services;
    }
}
