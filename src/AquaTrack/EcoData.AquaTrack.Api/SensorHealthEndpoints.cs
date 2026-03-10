using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Contracts.Parameters;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.AquaTrack.Api;

public static class SensorHealthEndpoints
{
    public static IEndpointRouteBuilder MapSensorHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/health/sensors").WithTags("Sensor Health");

        group.MapGet("/", GetHealthStatuses).WithName("GetSensorHealthStatuses");
        group.MapGet("/summary", GetHealthSummary).WithName("GetSensorHealthSummary");
        group.MapGet("/alerts", GetHealthAlerts).WithName("GetSensorHealthAlerts");

        var sensorGroup = app.MapGroup("/api/sensors/{sensorId:guid}/health")
            .WithTags("Sensor Health");

        sensorGroup.MapGet("/", GetSensorHealth).WithName("GetSensorHealth");
        sensorGroup.MapGet("/config", GetSensorHealthConfig).WithName("GetSensorHealthConfig");
        sensorGroup
            .MapPut("/config", UpsertSensorHealthConfig)
            .WithName("UpsertSensorHealthConfig")
            .RequireAuthorization("Admin");

        return app;
    }

    private static IAsyncEnumerable<SensorHealthStatusDtoForList> GetHealthStatuses(
        [AsParameters] SensorHealthParameters parameters,
        ISensorHealthRepository repository,
        CancellationToken ct
    ) => repository.GetStatusesAsync(parameters, ct);

    private static async Task<SensorHealthSummaryDto> GetHealthSummary(
        ISensorHealthRepository repository,
        CancellationToken ct
    ) => await repository.GetSummaryAsync(ct);

    private static IAsyncEnumerable<SensorHealthAlertDtoForList> GetHealthAlerts(
        [AsParameters] SensorHealthAlertParameters parameters,
        ISensorHealthRepository repository,
        CancellationToken ct
    ) => repository.GetAlertsAsync(parameters, ct);

    private static async Task<IResult> GetSensorHealth(
        Guid sensorId,
        ISensorHealthRepository repository,
        CancellationToken ct
    )
    {
        var status = await repository.GetStatusByIdAsync(sensorId, ct);
        return status is null ? Results.NotFound() : Results.Ok(status);
    }

    private static async Task<IResult> GetSensorHealthConfig(
        Guid sensorId,
        ISensorHealthRepository repository,
        CancellationToken ct
    )
    {
        var config = await repository.GetConfigByIdAsync(sensorId, ct);
        return config is null ? Results.NotFound() : Results.Ok(config);
    }

    private static async Task<IResult> UpsertSensorHealthConfig(
        Guid sensorId,
        SensorHealthConfigDtoForCreate dto,
        ISensorHealthRepository repository,
        ISensorRepository sensorRepository,
        CancellationToken ct
    )
    {
        var sensor = await sensorRepository.GetByIdAsync(sensorId, ct);
        if (sensor is null)
        {
            return Results.NotFound("Sensor not found");
        }

        var config = await repository.UpsertConfigAsync(sensorId, dto, ct);
        return Results.Ok(config);
    }
}
