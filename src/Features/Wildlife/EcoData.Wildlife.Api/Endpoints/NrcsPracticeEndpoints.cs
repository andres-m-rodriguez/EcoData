using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using EcoData.Common.Problems;
using EcoData.Common.Problems.AspNetCore;

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

        nrcsGroup
            .MapGet(
                "/{code}",
                async Task<Results<Ok<NrcsPracticeDtoForDetail>, JsonHttpResult<EcoDataProblemDetails>>> (
                    string code,
                    INrcsPracticeRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var practice = await repository.GetByCodeAsync(code, ct);
                    return practice is null
                        ? ProblemResults.NotFound($"Practice '{code}' was not found.")
                        : TypedResults.Ok(practice);
                }
            )
            .WithName("GetNrcsPracticeByCode");

        return app;
    }
}
