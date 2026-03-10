using System.Security.Claims;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.Identity.Contracts.Authorization;
using EcoData.AquaTrack.DataAccess.Interfaces;
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

        group.MapPost("/readings", SubmitReadings).WithName("SubmitReadings");
        group.MapPost("/readings/batch", SubmitReadingsBatch).WithName("SubmitReadingsBatch");
        group.MapPost("/heartbeat/{sensorId:guid}", SendHeartbeat).WithName("SendHeartbeat");

        return app;
    }

    private static async Task<IResult> SubmitReadings(
        ReadingBatchDtoForCreate batch,
        HttpContext httpContext,
        ISensorRepository sensorRepository,
        IReadingRepository readingRepository,
        ISensorHealthRepository healthRepository,
        CancellationToken ct
    )
    {
        var organizationId = GetOrganizationId(httpContext);
        if (organizationId is null)
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

    private static async Task<IResult> SubmitReadingsBatch(
        MultipleSensorReadingBatch multipleBatch,
        HttpContext httpContext,
        ISensorRepository sensorRepository,
        IReadingRepository readingRepository,
        ISensorHealthRepository healthRepository,
        CancellationToken ct
    )
    {
        var organizationId = GetOrganizationId(httpContext);
        if (organizationId is null)
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

    private static async Task<IResult> SendHeartbeat(
        Guid sensorId,
        HttpContext httpContext,
        ISensorRepository sensorRepository,
        ISensorHealthRepository healthRepository,
        CancellationToken ct
    )
    {
        var organizationId = GetOrganizationId(httpContext);
        if (organizationId is null)
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

    private static Guid? GetOrganizationId(HttpContext httpContext)
    {
        var orgClaim = httpContext.User.FindFirst("OrganizationId")?.Value;
        return Guid.TryParse(orgClaim, out var orgId) ? orgId : null;
    }
}
