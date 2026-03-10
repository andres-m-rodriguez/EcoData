using EcoData.Locations.Contracts.Parameters;
using EcoData.Locations.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Locations.Api;

public static class StateEndpoints
{
    public static IEndpointRouteBuilder MapStateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/states").WithTags("States");

        group
            .MapGet(
                "/",
                (
                    [AsParameters] StateParameters parameters,
                    IStateRepository repository,
                    CancellationToken ct
                ) => repository.GetStatesAsync(parameters, ct)
            )
            .WithName("GetStates");

        group
            .MapGet(
                "/{id:guid}",
                async (Guid id, IStateRepository repository, CancellationToken ct) =>
                {
                    var state = await repository.GetByIdAsync(id, ct);
                    return state is not null ? Results.Ok(state) : Results.NotFound();
                }
            )
            .WithName("GetStateById");

        group
            .MapGet(
                "/code/{code}",
                async (string code, IStateRepository repository, CancellationToken ct) =>
                {
                    var state = await repository.GetByCodeAsync(code, ct);
                    return state is not null ? Results.Ok(state) : Results.NotFound();
                }
            )
            .WithName("GetStateByCode");

        return app;
    }
}
