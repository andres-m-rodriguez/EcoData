using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EcoData.Sensors.Api.RateLimiting;

public static class SensorReadingsRateLimiterExtensions
{
    public const string SensorReadingsRateLimiterPolicy = "SensorReadingsRateLimiter";
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private const int MaxRequestsPerMinute = 10;

    public static IServiceCollection AddSensorReadingsRateLimiting(this IServiceCollection services)
    {
        services.Configure<RateLimiterOptions>(options =>
        {
            options.AddPolicy(SensorReadingsRateLimiterPolicy, context =>
            {
                // Partition by sensor ID from the route
                var sensorId = context.Request.RouteValues["sensorId"]?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    sensorId,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = MaxRequestsPerMinute,
                        Window = Window,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }
                );
            });
        });

        return services;
    }
}
