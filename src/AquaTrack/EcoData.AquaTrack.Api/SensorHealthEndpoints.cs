using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.Identity.Contracts.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.AquaTrack.Api;

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
                async (Guid sensorId, ISensorHealthRepository repository, CancellationToken ct) =>
                {
                    var status = await repository.GetStatusByIdAsync(sensorId, ct);
                    return status is null ? Results.NotFound() : Results.Ok(status);
                }
            )
            .WithName("GetSensorHealth");

        sensorGroup
            .MapGet(
                "/config",
                async (Guid sensorId, ISensorHealthRepository repository, CancellationToken ct) =>
                {
                    var config = await repository.GetConfigByIdAsync(sensorId, ct);
                    return config is null ? Results.NotFound() : Results.Ok(config);
                }
            )
            .WithName("GetSensorHealthConfig");

        sensorGroup
            .MapPut(
                "/config",
                async (
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
                        return Results.NotFound("Sensor not found");
                    }

                    var config = await repository.UpsertConfigAsync(sensorId, dto, ct);
                    return Results.Ok(config);
                }
            )
            .WithName("UpsertSensorHealthConfig")
            .RequireAuthorization(PolicyNames.Admin);

        return app;
    }
}
