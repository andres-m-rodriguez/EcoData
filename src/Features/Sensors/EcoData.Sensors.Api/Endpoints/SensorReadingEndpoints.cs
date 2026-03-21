using EcoData.Common.Messaging;
using EcoData.Sensors.Contracts;
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
            .MapGet(
                "/stream",
                (
                    Guid sensorId,
                    IMessageBroker<ReadingDtoForCreate> messageBroker,
                    CancellationToken ct
                ) =>
                {
                    var topic = sensorId.ToString();
                    var stream = StreamReadingsAsync(messageBroker, topic, ct);
                    return TypedResults.ServerSentEvents(stream, eventType: SseEventTypes.Reading);
                }
            )
            .WithName("StreamSensorReadings");

        group
            .MapPost(
                "/",
                async Task<Results<Ok<ReadingBatchResult>, ProblemHttpResult>> (
                    Guid sensorId,
                    ReadingBatchDtoForCreate batch,
                    ISensorRepository sensorRepository,
                    IReadingRepository readingRepository,
                    ISensorHealthRepository healthRepository,
                    IMessageBroker<ReadingDtoForCreate> messageBroker,
                    TimeProvider timeProvider,
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

                    var validator = new ReadingItemValidator(timeProvider.GetUtcNow());
                    var validReadings = new List<ReadingDtoForCreate>();
                    var errors = new List<string>();

                    foreach (var reading in batch.Readings)
                    {
                        var result = validator.Validate(reading);
                        if (!result.IsValid)
                        {
                            errors.AddRange(
                                result.Errors.Select(e =>
                                    $"'{reading.Parameter}': {e.ErrorMessage}"
                                )
                            );
                            continue;
                        }

                        validReadings.Add(
                            new ReadingDtoForCreate(
                                sensorId,
                                reading.Parameter,
                                reading.Description,
                                reading.Value,
                                reading.Unit,
                                reading.RecordedAt
                            )
                        );
                    }

                    if (validReadings.Count > 0)
                    {
                        await readingRepository.CreateManyAsync(validReadings, ct);

                        var maxRecordedAt = validReadings.Max(r => r.RecordedAt);
                        await healthRepository.RecordReadingAsync(sensorId, maxRecordedAt, ct);

                        // Publish readings to SSE subscribers
                        var topic = sensorId.ToString();
                        foreach (var reading in validReadings)
                        {
                            await messageBroker.PublishAsync(topic, reading, ct);
                        }
                    }

                    return TypedResults.Ok(
                        new ReadingBatchResult(
                            batch.Readings.Count,
                            validReadings.Count,
                            errors.Count,
                            errors
                        )
                    );
                }
            )
            .RequireAuthorization(policy =>
                policy.AddAuthenticationSchemes(SensorJwtScheme).RequireAuthenticatedUser()
            )
            .WithName("SubmitReadings");

        return app;
    }

    private static IAsyncEnumerable<ReadingDtoForCreate> StreamReadingsAsync(
        IMessageBroker<ReadingDtoForCreate> messageBroker,
        string topic,
        CancellationToken ct
    ) => messageBroker.SubscribeAsync(topic, ct);
}
