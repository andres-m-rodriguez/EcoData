using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api;

public static class SensorHealthEndpoints
{
    public static IEndpointRouteBuilder MapSensorHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/health/sensors").WithTags("Sensor Health");

        group
            .MapGet(
                "/",
                (
                    [AsParameters] SensorHealthParameters parameters,
                    ISensorHealthRepository repository,
                    CancellationToken ct
                ) => repository.GetStatusesAsync(parameters, ct)
            )
            .WithName("GetSensorHealthStatuses");

        group
            .MapGet(
                "/summary",
                (ISensorHealthRepository repository, CancellationToken ct) => repository.GetSummaryAsync(ct)
            )
            .WithName("GetSensorHealthSummary");

        group
            .MapGet(
                "/alerts",
                (
                    [AsParameters] SensorHealthAlertParameters parameters,
                    ISensorHealthRepository repository,
                    CancellationToken ct
                ) => repository.GetAlertsAsync(parameters, ct)
            )
            .WithName("GetSensorHealthAlerts");

        var sensorGroup = app.MapGroup("/api/sensors/{sensorId:guid}/health")
            .WithTags("Sensor Health");

        sensorGroup
            .MapGet(
                "/",
                async Task<Results<Ok<SensorHealthStatusDtoForDetail>, NotFound>> (
                    Guid sensorId,
                    ISensorHealthRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var status = await repository.GetStatusByIdAsync(sensorId, ct);
                    return status is null ? TypedResults.NotFound() : TypedResults.Ok(status);
                }
            )
            .WithName("GetSensorHealth");

        sensorGroup
            .MapGet(
                "/config",
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

        sensorGroup
            .MapPut(
                "/config",
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
            .WithName("UpsertSensorHealthConfig")
            .RequireAuthorization();

        return app;
    }
}
