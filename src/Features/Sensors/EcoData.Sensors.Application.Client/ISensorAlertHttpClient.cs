using EcoData.Common.Problems.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Parameters;
using OneOf;

namespace EcoData.Sensors.Application.Client;

public interface ISensorAlertHttpClient
{
    IAsyncEnumerable<SensorHealthAlertDtoForList> GetAlertsAsync(
        SensorHealthAlertParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<SensorHealthAlertDtoForDetail, ProblemDetail>> GetAlertByIdAsync(
        Guid alertId,
        CancellationToken cancellationToken = default
    );
}
