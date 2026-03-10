using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.AquaTrack.Api;

public static class ReferenceDataEndpoints
{
    public static IEndpointRouteBuilder MapReferenceDataEndpoints(this IEndpointRouteBuilder app)
    {
        var sensorTypesGroup = app.MapGroup("/api/sensor-types").WithTags("Sensor Types");

        sensorTypesGroup.MapGet("/", GetSensorTypes).WithName("GetSensorTypes");
        sensorTypesGroup.MapGet("/{id:guid}", GetSensorTypeById).WithName("GetSensorTypeById");

        var parametersGroup = app.MapGroup("/api/parameters").WithTags("Parameters");

        parametersGroup.MapGet("/", GetParameters).WithName("GetParameters");
        parametersGroup.MapGet("/{id:guid}", GetParameterById).WithName("GetParameterById");

        return app;
    }

    private static async Task<IReadOnlyList<SensorTypeDtoForList>> GetSensorTypes(
        ISensorTypeRepository repository,
        CancellationToken ct
    ) => await repository.GetAllAsync(ct);

    private static async Task<IResult> GetSensorTypeById(
        Guid id,
        ISensorTypeRepository repository,
        CancellationToken ct
    )
    {
        var sensorType = await repository.GetByIdAsync(id, ct);
        return sensorType is null ? Results.NotFound() : Results.Ok(sensorType);
    }

    private static async Task<IResult> GetParameters(
        Guid? sensorTypeId,
        IParameterRepository repository,
        CancellationToken ct
    )
    {
        IReadOnlyList<ParameterDtoForList> parameters;
        if (sensorTypeId.HasValue)
        {
            parameters = await repository.GetBySensorTypeAsync(sensorTypeId.Value, ct);
        }
        else
        {
            parameters = await repository.GetAllAsync(ct);
        }
        return Results.Ok(parameters);
    }

    private static async Task<IResult> GetParameterById(
        Guid id,
        IParameterRepository repository,
        CancellationToken ct
    )
    {
        var parameter = await repository.GetByIdAsync(id, ct);
        return parameter is null ? Results.NotFound() : Results.Ok(parameter);
    }
}
