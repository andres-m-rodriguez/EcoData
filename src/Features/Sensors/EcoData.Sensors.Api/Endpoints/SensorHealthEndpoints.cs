using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api.Endpoints;

public static class SensorHealthEndpoints
{
    private const string SensorJwtScheme = "SensorJwt";

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
                (ISensorHealthRepository repository, CancellationToken ct) =>
                    repository.GetSummaryAsync(ct)
            )
            .WithName("GetSensorHealthSummary");

        var sensorGroup = app.MapGroup("/api/sensors/{sensorId:guid}/health")
            .WithTags("Sensor Health");

        sensorGroup
            .MapGet(
                "/",
                async Task<Results<Ok<SensorHealthStatusDtoForDetail>, ProblemHttpResult>> (
                    Guid sensorId,
                    ISensorHealthRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var status = await repository.GetStatusByIdAsync(sensorId, ct);
                    return status is null
                        ? TypedResults.Problem(
                            detail: "Sensor health status not found",
                            statusCode: StatusCodes.Status404NotFound
                        )
                        : TypedResults.Ok(status);
                }
            )
            .WithName("GetSensorHealth");

        sensorGroup
            .MapPost(
                "/heartbeat",
                async Task<Results<Ok<HeartbeatResponse>, ProblemHttpResult>> (
                    Guid sensorId,
                    ISensorRepository sensorRepository,
                    ISensorHealthRepository healthRepository,
                    CancellationToken ct
                ) =>
                {
                    var sensor = await sensorRepository.GetByIdAsync(sensorId, ct);
                    if (sensor is null)
                    {
                        return TypedResults.Problem(
                            detail: $"Sensor {sensorId} not found",
                            statusCode: StatusCodes.Status404NotFound
                        );
                    }

                    await healthRepository.RecordReadingAsync(sensorId, DateTimeOffset.UtcNow, ct);

                    return TypedResults.Ok(
                        new HeartbeatResponse("Heartbeat received", sensorId, DateTimeOffset.UtcNow)
                    );
                }
            )
            .RequireAuthorization(policy =>
                policy.AddAuthenticationSchemes(SensorJwtScheme).RequireAuthenticatedUser()
            )
            .WithName("SendHeartbeat");

        return app;
    }
}
