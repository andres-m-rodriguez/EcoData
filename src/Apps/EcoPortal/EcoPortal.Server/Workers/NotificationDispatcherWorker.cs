using EcoData.Common.Messaging.Abstractions;
using EcoData.Sensors.Contracts;
using EcoData.Sensors.Contracts.Events;
using EcoPortal.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoPortal.Server.Workers;

public sealed class NotificationDispatcherWorker(
    IServiceScopeFactory scopeFactory,
    IMessageBus messageBus,
    ILogger<NotificationDispatcherWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification Dispatcher Worker starting");

        try
        {
            await foreach (var alertEvent in messageBus
                .SubscribeToEventsAsync<SensorHealthAlertEvent>(
                    MessageTopics.AllHealthAlerts,
                    stoppingToken
                ))
            {
                try
                {
                    logger.LogDebug(
                        "Received health alert {AlertId} for sensor {SensorId}: {AlertType}",
                        alertEvent.Id,
                        alertEvent.SensorId,
                        alertEvent.AlertType
                    );

                    using var scope = scopeFactory.CreateScope();
                    var routingService = scope.ServiceProvider
                        .GetRequiredService<INotificationRoutingService>();

                    await routingService.RouteHealthAlertAsync(alertEvent, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error processing health alert {AlertId}",
                        alertEvent.Id
                    );
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in Notification Dispatcher Worker");
            throw;
        }

        logger.LogInformation("Notification Dispatcher Worker stopping");
    }
}
