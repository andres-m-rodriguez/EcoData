using EcoData.Sensors.DataAccess.Interfaces;
using EcoData.Sensors.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EcoData.Sensors.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddSensorsDataAccess(this IServiceCollection services)
    {
        services.AddScoped<ISensorRepository, SensorRepository>();
        services.AddScoped<ISensorTypeRepository, SensorTypeRepository>();
        services.AddScoped<IParameterRepository, ParameterRepository>();
        services.AddScoped<ISensorHealthRepository, SensorHealthRepository>();
        services.AddScoped<IReadingRepository, ReadingRepository>();
        services.AddScoped<IIngestionLogRepository, IngestionLogRepository>();
        services.AddScoped<IUserSensorSubscriptionRepository, UserSensorSubscriptionRepository>();
        services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();

        return services;
    }
}
