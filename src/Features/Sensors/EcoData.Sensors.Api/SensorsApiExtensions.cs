using EcoData.Sensors.Api.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api;

public static class SensorsApiExtensions
{
    public static IEndpointRouteBuilder MapSensorsApiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapSensorEndpoints();
        app.MapSensorReadingEndpoints();
        app.MapSensorHealthEndpoints();
        app.MapSensorHealthConfigEndpoints();
        app.MapReferenceDataEndpoints();

        return app;
    }
}
