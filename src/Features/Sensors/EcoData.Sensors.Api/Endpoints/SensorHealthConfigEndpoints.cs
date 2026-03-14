using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api.Endpoints;

public static class SensorHealthConfigEndpoints
{
    public static IEndpointRouteBuilder MapSensorHealthConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sensors/{sensorId:guid}/health/config")
            .WithTags("Sensor Health Config");

        group
            .MapGet(
                "/",
                async Task<Results<Ok<SensorHealthConfigDtoForDetail>, NotFound>> (
                    Guid sensorId,
                    ISensorHealthRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var config = await repository.GetConfigByIdAsync(sensorId, ct);
                    return config is null ? TypedResults.NotFound() : TypedResults.Ok(config);
                }
            )
            .WithName("GetSensorHealthConfig");

        group
            .MapPut(
                "/",
                async Task<Results<Ok<SensorHealthConfigDtoForDetail>, NotFound<string>>> (
                    Guid sensorId,
                    SensorHealthConfigDtoForCreate dto,
                    ISensorHealthRepository repository,
                    ISensorRepository sensorRepository,
                    CancellationToken ct
                ) =>
                {
                    var sensor = await sensorRepository.GetByIdAsync(sensorId, ct);
                    if (sensor is null)
                    {
                        return TypedResults.NotFound("Sensor not found");
                    }

                    var config = await repository.UpsertConfigAsync(sensorId, dto, ct);
                    return TypedResults.Ok(config);
                }
            )
            .RequireAuthorization()
            .WithName("UpsertSensorHealthConfig");

        return app;
    }
}
