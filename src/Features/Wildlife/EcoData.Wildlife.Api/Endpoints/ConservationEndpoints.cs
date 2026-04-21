using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Wildlife.Api.Endpoints;

public static class ConservationEndpoints
{
    public static IEndpointRouteBuilder MapConservationEndpoints(this IEndpointRouteBuilder app)
    {
        var fwsGroup = app.MapGroup("/wildlife/fws-actions").WithTags("FWS Actions");

        fwsGroup
            .MapGet(
                "/",
                async Task<Ok<IReadOnlyList<FwsActionDtoForList>>> (
                    IConservationRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var actions = await repository.GetAllFwsActionsAsync(ct);
                    return TypedResults.Ok(actions);
                }
            )
            .WithName("GetFwsActions");

        var nrcsGroup = app.MapGroup("/wildlife/nrcs-practices").WithTags("NRCS Practices");

        nrcsGroup
            .MapGet(
                "/",
                async Task<Ok<IReadOnlyList<NrcsPracticeDtoForList>>> (
                    IConservationRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var practices = await repository.GetAllNrcsPracticesAsync(ct);
                    return TypedResults.Ok(practices);
                }
            )
            .WithName("GetNrcsPractices");

        var linksGroup = app.MapGroup("/wildlife/conservation-links").WithTags("Conservation Links");

        linksGroup
            .MapGet(
                "/species/{speciesId:guid}",
                async Task<Ok<ConservationLinksDtoForSpecies>> (
                    Guid speciesId,
                    IConservationRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var links = await repository.GetLinksForSpeciesAsync(speciesId, ct);
                    return TypedResults.Ok(links);
                }
            )
            .WithName("GetConservationLinksForSpecies");

        return app;
    }
}
