using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api;

public static class SensorsApiExtensions
{
    public static IEndpointRouteBuilder MapSensorsApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapSensorEndpoints();
        app.MapSensorHealthEndpoints();
        app.MapReferenceDataEndpoints();
        app.MapPushEndpoints();

        return app;
    }
}
