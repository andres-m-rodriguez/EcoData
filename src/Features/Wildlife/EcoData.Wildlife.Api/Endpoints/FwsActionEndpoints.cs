using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Wildlife.Api.Endpoints;

public static class FwsActionEndpoints
{
    public static IEndpointRouteBuilder MapFwsActionEndpoints(this IEndpointRouteBuilder app)
    {
        var fwsGroup = app.MapGroup("/wildlife/fws-actions").WithTags("FWS Actions");

        fwsGroup
            .MapGet(
                "/",
                async Task<Ok<IReadOnlyList<FwsActionDtoForList>>> (
                    IFwsActionRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var actions = await repository.GetAsync(ct);
                    return TypedResults.Ok(actions);
                }
            )
            .WithName("GetFwsActions");

        return app;
    }
}
