using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api.Endpoints;

public static class SensorReadingEndpoints
{
    private const string SensorJwtScheme = "SensorJwt";

    public static IEndpointRouteBuilder MapSensorReadingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sensors/{sensorId:guid}/readings")
            .WithTags("Sensor Readings");

        group
            .MapGet(
                "/",
                (
                    Guid sensorId,
                    [AsParameters] ReadingParameters parameters,
                    IReadingRepository repository,
                    CancellationToken ct
                ) => repository.GetReadingsAsync(sensorId, parameters, ct)
            )
            .WithName("GetSensorReadings");

        group
            .MapGet(
                "/parameters",
                (Guid sensorId, IReadingRepository repository, CancellationToken ct) =>
                    repository.GetDistinctParametersAsync(sensorId, ct)
            )
            .WithName("GetSensorReadingParameters");

        group
            .MapPost(
                "/",
                async Task<Results<Ok<ReadingBatchResult>, NotFound<string>>> (
                    Guid sensorId,
                    ReadingBatchDtoForCreate batch,
                    ISensorRepository sensorRepository,
                    IReadingRepository readingRepository,
                    ISensorHealthRepository healthRepository,
                    CancellationToken ct
                ) =>
                {
                    var sensor = await sensorRepository.GetByIdAsync(sensorId, ct);
                    if (sensor is null)
                    {
                        return TypedResults.NotFound($"Sensor {sensorId} not found");
                    }

                    var readingsToCreate = batch
                        .Readings.Select(reading => new ReadingDtoForCreate(
                            sensorId,
                            reading.Parameter,
                            reading.Description,
                            reading.Value,
                            reading.Unit,
                            reading.RecordedAt
                        ))
                        .ToList();

                    if (readingsToCreate.Count > 0)
                    {
                        await readingRepository.CreateManyAsync(readingsToCreate, ct);

                        var maxRecordedAt = readingsToCreate.Max(r => r.RecordedAt);
                        await healthRepository.RecordReadingAsync(sensorId, maxRecordedAt, ct);
                    }

                    return TypedResults.Ok(
                        new ReadingBatchResult(batch.Readings.Count, readingsToCreate.Count, 0, [])
                    );
                }
            )
            .RequireAuthorization(policy =>
                policy.AddAuthenticationSchemes(SensorJwtScheme).RequireAuthenticatedUser()
            )
            .WithName("SubmitReadings");

        return app;
    }
}
