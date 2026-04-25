using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api.Endpoints;

public static class ReferenceDataEndpoints
{
    public static IEndpointRouteBuilder MapReferenceDataEndpoints(this IEndpointRouteBuilder app)
    {
        var sensorTypesGroup = app.MapGroup("/sensors/types").WithTags("Sensor Types");

        sensorTypesGroup
            .MapGet(
                "/",
                (ISensorTypeRepository repository, CancellationToken ct) =>
                    repository.GetAllAsync(ct)
            )
            .WithName("GetSensorTypes");

        sensorTypesGroup
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<SensorTypeDtoForDetail>, NotFound>> (
                    Guid id,
                    ISensorTypeRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var sensorType = await repository.GetByIdAsync(id, ct);
                    return sensorType is null
                        ? TypedResults.NotFound()
                        : TypedResults.Ok(sensorType);
                }
            )
            .WithName("GetSensorTypeById");

        var parametersGroup = app.MapGroup("/sensors/parameters").WithTags("Parameters");

        parametersGroup
            .MapGet(
                "/",
                async (Guid? sensorTypeId, IParameterRepository repository, CancellationToken ct) =>
                {
                    var parameters = sensorTypeId.HasValue
                        ? await repository.GetBySensorTypeAsync(sensorTypeId.Value, ct)
                        : await repository.GetAllAsync(ct);
                    return TypedResults.Ok(parameters);
                }
            )
            .WithName("GetParameters");

        parametersGroup
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<ParameterDtoForDetail>, NotFound>> (
                    Guid id,
                    IParameterRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var parameter = await repository.GetByIdAsync(id, ct);
                    return parameter is null ? TypedResults.NotFound() : TypedResults.Ok(parameter);
                }
            )
            .WithName("GetParameterById");

        return app;
    }
}
