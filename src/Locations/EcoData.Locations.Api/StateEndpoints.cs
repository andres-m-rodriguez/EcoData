using EcoData.Locations.Contracts.Dtos;
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

        group.MapGet("/", GetStates).WithName("GetStates");
        group.MapGet("/{id:guid}", GetStateById).WithName("GetStateById");
        group.MapGet("/code/{code}", GetStateByCode).WithName("GetStateByCode");

        return app;
    }

    private static IAsyncEnumerable<StateDtoForList> GetStates(
        [AsParameters] StateParameters parameters,
        IStateRepository repository,
        CancellationToken ct
    ) => repository.GetStatesAsync(parameters, ct);

    private static async Task<IResult> GetStateById(
        Guid id,
        IStateRepository repository,
        CancellationToken ct
    )
    {
        var state = await repository.GetByIdAsync(id, ct);
        return state is not null ? Results.Ok(state) : Results.NotFound();
    }

    private static async Task<IResult> GetStateByCode(
        string code,
        IStateRepository repository,
        CancellationToken ct
    )
    {
        var state = await repository.GetByCodeAsync(code, ct);
        return state is not null ? Results.Ok(state) : Results.NotFound();
    }
}
