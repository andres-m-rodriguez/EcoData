using EcoData.Sensors.Contracts.Dtos;
using EcoData.Sensors.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Sensors.Api.Endpoints;

public static class PhenomenonEndpoints
{
    public static IEndpointRouteBuilder MapPhenomenonEndpoints(this IEndpointRouteBuilder app)
    {
        var phenomenaGroup = app.MapGroup("/sensors/phenomena").WithTags("Phenomena");

        phenomenaGroup
            .MapGet(
                "/",
                (string? capability, IPhenomenonRepository repository, CancellationToken ct) =>
                    string.IsNullOrWhiteSpace(capability)
                        ? repository.GetAllAsync(ct)
                        : repository.GetByCapabilityAsync(capability, ct)
            )
            .WithName("GetPhenomena");

        phenomenaGroup
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<PhenomenonDtoForDetail>, NotFound>> (
                    Guid id,
                    IPhenomenonRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var phenomenon = await repository.GetByIdAsync(id, ct);
                    return phenomenon is null
                        ? TypedResults.NotFound()
                        : TypedResults.Ok(phenomenon);
                }
            )
            .WithName("GetPhenomenonById");

        return app;
    }
}
