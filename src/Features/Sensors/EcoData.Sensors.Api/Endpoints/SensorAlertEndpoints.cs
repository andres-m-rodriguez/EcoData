using EcoData.Common.Messaging.Abstractions;
using EcoData.Sensors.Contracts;
using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.Contracts.Events;
using EcoData.Sensors.Contracts.Parameters;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api.Endpoints;

public static class SensorAlertEndpoints
{
    public static IEndpointRouteBuilder MapSensorAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sensors/alerts").WithTags("Sensor Alerts");

        group
            .MapGet(
                "/",
                (
                    [AsParameters] SensorHealthAlertParameters parameters,
                    ISensorHealthRepository repository,
                    CancellationToken ct
                ) => repository.GetAlertsAsync(parameters, ct)
            )
            .WithName("GetSensorAlerts");

        group
            .MapGet(
                "/{alertId:guid}",
                async Task<Results<Ok<SensorHealthAlertDtoForDetail>, ProblemHttpResult>> (
                    Guid alertId,
                    ISensorHealthRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var alert = await repository.GetAlertByIdAsync(alertId, ct);
                    return alert is null
                        ? TypedResults.Problem(
                            detail: "Alert not found",
                            statusCode: StatusCodes.Status404NotFound
                        )
                        : TypedResults.Ok(alert);
                }
            )
            .WithName("GetSensorAlertById");

        return app;
    }
}
