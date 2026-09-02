using System.Security.Claims;
using EcoData.Common.Authorization;
using EcoData.Identity.Contracts.Claims;
using EcoData.Wildlife.Application;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using EcoData.Wildlife.Contracts.Validators;
using EcoData.Wildlife.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Wildlife.Api.Endpoints;

public static class SightingEndpoints
{
    public static IEndpointRouteBuilder MapSightingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/wildlife").WithTags("Sightings");

        // Any signed-in account may report; membership in the organization is
        // not required and wildlife:occurrence:submit stays unenforced for now.
        group
            .MapPost(
                "/organizations/{organizationId:guid}/sightings",
                async Task<
                    Results<Created<SightingDto>, ValidationProblem, NotFound, UnauthorizedHttpResult>
                > (
                    Guid organizationId,
                    SightingDtoForCreate dto,
                    ClaimsPrincipal user,
                    ISightingRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var caller = new RequestClaimToken(user);
                    if (!caller.IsAuthenticated)
                        return TypedResults.Unauthorized();

                    var validation = new SightingDtoForCreateValidator().Validate(dto);
                    if (!validation.IsValid)
                        return TypedResults.ValidationProblem(validation.ToDictionary());

                    var result = await repository.CreateAsync(
                        organizationId,
                        caller.UserId.Value,
                        caller.DisplayName,
                        dto,
                        ct
                    );
                    if (result.IsT1)
                        return TypedResults.NotFound();

                    var sighting = result.AsT0;
                    return TypedResults.Created(
                        $"/wildlife/organizations/{organizationId}/sightings/{sighting.Id}",
                        sighting
                    );
                }
            )
            .RequireAuthorization()
            .WithName("ReportSighting");

        group
            .MapGet(
                "/me/sightings",
                (
                    [AsParameters] SightingParameters parameters,
                    ClaimsPrincipal user,
                    ISightingRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var caller = new RequestClaimToken(user);
                    return repository.GetMineAsync(caller.UserId!.Value, parameters, ct);
                }
            )
            .RequireAuthorization()
            .WithName("GetMySightings");

        // One thread serves both sides: the reporter adds detail, a reviewer in
        // the sighting's organization asks for it.
        group
            .MapPost(
                "/sightings/{id:guid}/notes",
                async Task<
                    Results<Created<SightingNoteDto>, ValidationProblem, NotFound, ForbidHttpResult>
                > (
                    Guid id,
                    SightingNoteDtoForCreate dto,
                    ClaimsPrincipal user,
                    IAuthorization auth,
                    ISightingRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var owner = await repository.GetOwnerAsync(id, ct);
                    if (owner is null)
                        return TypedResults.NotFound();

                    var caller = new RequestClaimToken(user);
                    var isReporter = caller.UserId == owner.Value.ReporterUserId;
                    if (
                        !isReporter
                        && !await auth.HasAsync(
                            WildlifePermissions.VerifyOccurrence,
                            owner.Value.OrganizationId,
                            ct
                        )
                    )
                        return TypedResults.Forbid();

                    var validation = new SightingNoteDtoForCreateValidator().Validate(dto);
                    if (!validation.IsValid)
                        return TypedResults.ValidationProblem(validation.ToDictionary());

                    var result = await repository.AddNoteAsync(
                        id,
                        caller.UserId!.Value,
                        caller.DisplayName,
                        dto,
                        ct
                    );
                    if (result.IsT1)
                        return TypedResults.NotFound();

                    return TypedResults.Created(
                        $"/wildlife/organizations/{owner.Value.OrganizationId}/sightings/{id}",
                        result.AsT0
                    );
                }
            )
            .RequireAuthorization()
            .WithName("AddSightingNote");

        return app;
    }
}
