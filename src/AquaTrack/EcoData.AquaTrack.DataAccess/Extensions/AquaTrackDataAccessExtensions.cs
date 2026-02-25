using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.AquaTrack.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.AquaTrack.DataAccess.Extensions;

public static class AquaTrackDataAccessExtensions
{
    public static IServiceCollection AddAquaTrackDataAccess(this IServiceCollection services)
    {
        services.AddScoped<IDataSourceRepository, DataSourceRepository>();
        services.AddScoped<ISensorRepository, SensorRepository>();
        services.AddScoped<IReadingRepository, ReadingRepository>();
        services.AddScoped<IIngestionLogRepository, IngestionLogRepository>();

        return services;
    }
}
