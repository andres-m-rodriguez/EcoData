using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api;

public static class SensorEndpoints
{
    public static IEndpointRouteBuilder MapSensorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sensors").WithTags("Sensors");

        group
            .MapGet(
                "/",
                (
                    [AsParameters] SensorParameters parameters,
                    ISensorRepository repository,
                    CancellationToken ct
                ) => repository.GetSensorsAsync(parameters, ct)
            )
            .WithName("GetSensors");

        group
            .MapGet(
                "/count",
                (
                    [AsParameters] SensorParameters parameters,
                    ISensorRepository repository,
                    CancellationToken ct
                ) => repository.GetSensorCountAsync(parameters, ct)
            )
            .WithName("GetSensorCount");

        group
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<SensorDtoForDetail>, NotFound>> (
                    Guid id,
                    ISensorRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var sensor = await repository.GetByIdAsync(id, ct);
                    return sensor is null ? TypedResults.NotFound() : TypedResults.Ok(sensor);
                }
            )
            .WithName("GetSensorById");

        return app;
    }
}
