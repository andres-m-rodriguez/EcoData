using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.AquaTrack.Api;

public static class SensorEndpoints
{
    public static IEndpointRouteBuilder MapSensorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sensors").WithTags("Sensors");

        group
            .MapGet(
                "/",
                ([AsParameters] SensorParameters parameters, ISensorRepository repository, CancellationToken ct) =>
                    repository.GetSensorsAsync(parameters, ct)
            )
            .WithName("GetSensors");

        return app;
    }
}
