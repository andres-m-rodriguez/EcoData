using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Wildlife.Api.Endpoints;

public static class ConservationLinkEndpoints
{
    public static IEndpointRouteBuilder MapConservationLinkEndpoints(this IEndpointRouteBuilder app)
    {
        var linksGroup = app.MapGroup("/wildlife/conservation-links").WithTags("Conservation Links");

        linksGroup
            .MapGet(
                "/species/{speciesId:guid}",
                async Task<Ok<ConservationLinksDtoForSpecies>> (
                    Guid speciesId,
                    IConservationLinkRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var links = await repository.GetBySpeciesAsync(speciesId, ct);
                    return TypedResults.Ok(links);
                }
            )
            .WithName("GetConservationLinksForSpecies");

        linksGroup
            .MapGet(
                "/codes-by-municipality/{municipalityId:guid}",
                async Task<Ok<IReadOnlyList<SpeciesConservationCodesDto>>> (
                    Guid municipalityId,
                    IConservationLinkRepository repository,
                    CancellationToken ct
                ) => TypedResults.Ok(await repository.GetCodesByMunicipalityAsync(municipalityId, ct))
            )
            .WithName("GetConservationCodesByMunicipality");

        return app;
    }
}
