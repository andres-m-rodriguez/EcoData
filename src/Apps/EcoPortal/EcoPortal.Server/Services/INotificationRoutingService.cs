using EcoData.Sensors.Contracts.Events;

namespace EcoPortal.Server.Services;

public interface INotificationRoutingService
{
    Task RouteHealthAlertAsync(
        SensorHealthAlertEvent alertEvent,
        CancellationToken cancellationToken = default
    );
}
