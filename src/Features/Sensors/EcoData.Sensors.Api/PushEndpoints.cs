using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api;

public static class PushEndpoints
{
    private const string SensorJwtScheme = "SensorJwt";

    public static IEndpointRouteBuilder MapPushEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/push")
            .WithTags("Push API")
            .RequireAuthorization(policy =>
                policy.AddAuthenticationSchemes(SensorJwtScheme)
                      .RequireAuthenticatedUser());

        group
            .MapPost(
                "/readings",
                async Task<
                    Results<Ok<ReadingBatchResult>, UnauthorizedHttpResult, NotFound<string>>
                > (
                    ReadingBatchDtoForCreate batch,
                    ISensorRepository sensorRepository,
                    IReadingRepository readingRepository,
                    ISensorHealthRepository healthRepository,
                    CancellationToken ct
                ) =>
                {
                    var sensor = await sensorRepository.GetByIdAsync(batch.SensorId, ct);
                    if (sensor is null)
                    {
                        return TypedResults.NotFound($"Sensor {batch.SensorId} not found");
                    }

                    var errors = new List<string>();
                    var accepted = 0;

                    var readingsToCreate = new List<ReadingDtoForCreate>();
                    foreach (var reading in batch.Readings)
                    {
                        readingsToCreate.Add(
                            new ReadingDtoForCreate(
                                batch.SensorId,
                                reading.Parameter,
                                reading.Value,
                                reading.Unit,
                                reading.RecordedAt
                            )
                        );
                        accepted++;
                    }

                    if (readingsToCreate.Count > 0)
                    {
                        await readingRepository.CreateManyAsync(readingsToCreate, ct);

                        var maxRecordedAt = readingsToCreate.Max(r => r.RecordedAt);
                        await healthRepository.RecordReadingAsync(
                            batch.SensorId,
                            maxRecordedAt,
                            ct
                        );
                    }

                    return TypedResults.Ok(
                        new ReadingBatchResult(batch.Readings.Count, accepted, errors.Count, errors)
                    );
                }
            )
            .WithName("SubmitReadings");

        group
            .MapPost(
                "/readings/batch",
                async Task<Ok<ReadingBatchResult>> (
                    MultipleSensorReadingBatch multipleBatch,
                    ISensorRepository sensorRepository,
                    IReadingRepository readingRepository,
                    ISensorHealthRepository healthRepository,
                    CancellationToken ct
                ) =>
                {
                    var totalAccepted = 0;
                    var totalRejected = 0;
                    var allErrors = new List<string>();

                    foreach (var batch in multipleBatch.Batches)
                    {
                        var sensor = await sensorRepository.GetByIdAsync(batch.SensorId, ct);
                        if (sensor is null)
                        {
                            allErrors.Add($"Sensor {batch.SensorId} not found");
                            totalRejected += batch.Readings.Count;
                            continue;
                        }

                        var readingsToCreate = new List<ReadingDtoForCreate>();
                        foreach (var reading in batch.Readings)
                        {
                            readingsToCreate.Add(
                                new ReadingDtoForCreate(
                                    batch.SensorId,
                                    reading.Parameter,
                                    reading.Value,
                                    reading.Unit,
                                    reading.RecordedAt
                                )
                            );
                            totalAccepted++;
                        }

                        if (readingsToCreate.Count > 0)
                        {
                            await readingRepository.CreateManyAsync(readingsToCreate, ct);

                            var maxRecordedAt = readingsToCreate.Max(r => r.RecordedAt);
                            await healthRepository.RecordReadingAsync(
                                batch.SensorId,
                                maxRecordedAt,
                                ct
                            );
                        }
                    }

                    var totalSubmitted = multipleBatch.Batches.Sum(b => b.Readings.Count);
                    return TypedResults.Ok(
                        new ReadingBatchResult(
                            totalSubmitted,
                            totalAccepted,
                            totalRejected,
                            allErrors
                        )
                    );
                }
            )
            .WithName("SubmitReadingsBatch");

        group
            .MapPost(
                "/heartbeat/{sensorId:guid}",
                async Task<Results<Ok<HeartbeatResult>, NotFound<string>>> (
                    Guid sensorId,
                    ISensorRepository sensorRepository,
                    ISensorHealthRepository healthRepository,
                    CancellationToken ct
                ) =>
                {
                    var sensor = await sensorRepository.GetByIdAsync(sensorId, ct);
                    if (sensor is null)
                    {
                        return TypedResults.NotFound($"Sensor {sensorId} not found");
                    }

                    await healthRepository.RecordReadingAsync(sensorId, DateTimeOffset.UtcNow, ct);

                    return TypedResults.Ok(
                        new HeartbeatResult("Heartbeat received", sensorId, DateTimeOffset.UtcNow)
                    );
                }
            )
            .WithName("SendHeartbeat");

        return app;
    }
}

public record HeartbeatResult(string Message, Guid SensorId, DateTimeOffset Timestamp);
