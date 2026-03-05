using EcoData.Identity.DataAccess.Interfaces;
using EcoData.Identity.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Identity.DataAccess.Extensions;

public static class IdentityDataAccessExtensions
{
    public static IServiceCollection AddIdentityDataAccess(this IServiceCollection services)
    {
        services.AddScoped<IAccessRequestRepository, AccessRequestRepository>();

        return services;
    }
}
