using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Wildlife.Api.Endpoints;

public static class NrcsPracticeEndpoints
{
    public static IEndpointRouteBuilder MapNrcsPracticeEndpoints(this IEndpointRouteBuilder app)
    {
        var nrcsGroup = app.MapGroup("/wildlife/nrcs-practices").WithTags("NRCS Practices");

        nrcsGroup
            .MapGet(
                "/",
                async Task<Ok<IReadOnlyList<NrcsPracticeDtoForList>>> (
                    INrcsPracticeRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var practices = await repository.GetListAsync(ct);
                    return TypedResults.Ok(practices);
                }
            )
            .WithName("GetNrcsPractices");

        return app;
    }
}
