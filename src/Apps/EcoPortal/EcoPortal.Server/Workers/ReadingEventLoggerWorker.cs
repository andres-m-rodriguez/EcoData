using EcoData.Common.Messaging.Abstractions;
using EcoData.Sensors.Contracts.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoPortal.Server.Workers;

// Dev-only sanity check: tails ReadingCreatedEvent off the bus so we can
// confirm publishes from both the push API and the USGS pull worker land.
// Not registered in production — see Program.cs.
public sealed class ReadingEventLoggerWorker(
    IMessageBus messageBus,
    ILogger<ReadingEventLoggerWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Reading Event Logger Worker starting (dev only)");

        try
        {
            await foreach (var reading in messageBus.SubscribeToEventsAsync<ReadingCreatedEvent>(
                cancellationToken: stoppingToken))
            {
                logger.LogInformation(
                    "ReadingCreatedEvent: sensor={SensorId} parameter={Parameter} value={Value} {Unit} recordedAt={RecordedAt:O}",
                    reading.SensorId,
                    reading.Parameter,
                    reading.Value,
                    reading.Unit,
                    reading.RecordedAt
                );
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in Reading Event Logger Worker");
            throw;
        }

        logger.LogInformation("Reading Event Logger Worker stopping");
    }
}
