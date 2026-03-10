using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.Identity.Contracts.Authorization;
using EcoData.Identity.Contracts.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.AquaTrack.Api;

public static class PushEndpoints
{
    public static IEndpointRouteBuilder MapPushEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/push")
            .WithTags("Push API")
            .RequireAuthorization(PolicyNames.ApiKey);

        group
            .MapPost(
                "/readings",
                async (
                    ReadingBatchDtoForCreate batch,
                    HttpContext httpContext,
                    ISensorRepository sensorRepository,
                    IReadingRepository readingRepository,
                    ISensorHealthRepository healthRepository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(httpContext.User);
                    if (token.OrganizationId is null)
                    {
                        return Results.Unauthorized();
                    }

                    var sensor = await sensorRepository.GetByIdAsync(batch.SensorId, ct);
                    if (sensor is null)
                    {
                        return Results.NotFound($"Sensor {batch.SensorId} not found");
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
                        await healthRepository.RecordReadingAsync(batch.SensorId, maxRecordedAt, ct);
                    }

                    return Results.Ok(
                        new ReadingBatchResult(batch.Readings.Count, accepted, errors.Count, errors)
                    );
                }
            )
            .WithName("SubmitReadings");

        group
            .MapPost(
                "/readings/batch",
                async (
                    MultipleSensorReadingBatch multipleBatch,
                    HttpContext httpContext,
                    ISensorRepository sensorRepository,
                    IReadingRepository readingRepository,
                    ISensorHealthRepository healthRepository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(httpContext.User);
                    if (token.OrganizationId is null)
                    {
                        return Results.Unauthorized();
                    }

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
                            await healthRepository.RecordReadingAsync(batch.SensorId, maxRecordedAt, ct);
                        }
                    }

                    var totalSubmitted = multipleBatch.Batches.Sum(b => b.Readings.Count);
                    return Results.Ok(
                        new ReadingBatchResult(totalSubmitted, totalAccepted, totalRejected, allErrors)
                    );
                }
            )
            .WithName("SubmitReadingsBatch");

        group
            .MapPost(
                "/heartbeat/{sensorId:guid}",
                async (
                    Guid sensorId,
                    HttpContext httpContext,
                    ISensorRepository sensorRepository,
                    ISensorHealthRepository healthRepository,
                    CancellationToken ct
                ) =>
                {
                    var token = new RequestClaimToken(httpContext.User);
                    if (token.OrganizationId is null)
                    {
                        return Results.Unauthorized();
                    }

                    var sensor = await sensorRepository.GetByIdAsync(sensorId, ct);
                    if (sensor is null)
                    {
                        return Results.NotFound($"Sensor {sensorId} not found");
                    }

                    await healthRepository.RecordReadingAsync(sensorId, DateTimeOffset.UtcNow, ct);

                    return Results.Ok(
                        new
                        {
                            Message = "Heartbeat received",
                            SensorId = sensorId,
                            Timestamp = DateTimeOffset.UtcNow,
                        }
                    );
                }
            )
            .WithName("SendHeartbeat");

        return app;
    }
}
