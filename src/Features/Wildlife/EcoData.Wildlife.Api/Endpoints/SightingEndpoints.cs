using System.Security.Claims;
using EcoData.Common.Authorization;
using EcoData.Identity.Contracts.Claims;
using EcoData.Wildlife.Application;
using EcoData.Wildlife.Contracts;
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

        // Review queue for members holding wildlife:occurrence:verify in the
        // organization, and for global admins.
        var review = app.MapGroup("/wildlife/organizations/{organizationId:guid}/sightings")
            .WithTags("Sightings")
            .RequireAuthorization();

        // Materialized instead of streamed: an IAsyncEnumerable handler cannot
        // answer 403, and an empty stream would look like "no sightings".
        review
            .MapGet(
                "/",
                async Task<Results<Ok<IReadOnlyList<SightingDto>>, ForbidHttpResult>> (
                    Guid organizationId,
                    [AsParameters] SightingParameters parameters,
                    IAuthorization auth,
                    ISightingRepository repository,
                    CancellationToken ct
                ) =>
                {
                    if (!await auth.HasAsync(WildlifePermissions.VerifyOccurrence, organizationId, ct))
                        return TypedResults.Forbid();

                    var page = await repository
                        .GetByOrganizationAsync(organizationId, parameters, ct)
                        .ToListAsync(ct);
                    return TypedResults.Ok<IReadOnlyList<SightingDto>>(page);
                }
            )
            .WithName("GetOrganizationSightings");

        review
            .MapGet(
                "/count",
                async Task<Results<Ok<int>, ForbidHttpResult>> (
                    Guid organizationId,
                    SightingStatus? status,
                    IAuthorization auth,
                    ISightingRepository repository,
                    CancellationToken ct
                ) =>
                {
                    if (!await auth.HasAsync(WildlifePermissions.VerifyOccurrence, organizationId, ct))
                        return TypedResults.Forbid();

                    return TypedResults.Ok(await repository.CountAsync(organizationId, status, ct));
                }
            )
            .WithName("CountOrganizationSightings");

        review
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<SightingDto>, NotFound, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid id,
                    IAuthorization auth,
                    ISightingRepository repository,
                    CancellationToken ct
                ) =>
                {
                    if (!await auth.HasAsync(WildlifePermissions.VerifyOccurrence, organizationId, ct))
                        return TypedResults.Forbid();

                    var sighting = await repository.GetByIdAsync(organizationId, id, ct);
                    if (sighting is null)
                        return TypedResults.NotFound();

                    return TypedResults.Ok(sighting);
                }
            )
            .WithName("GetOrganizationSightingById");

        review
            .MapPost(
                "/{id:guid}/approve",
                async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem>> (
                    Guid organizationId,
                    Guid id,
                    SightingReviewDto dto,
                    ClaimsPrincipal user,
                    IAuthorization auth,
                    ISightingRepository repository,
                    CancellationToken ct
                ) =>
                {
                    if (!await auth.HasAsync(WildlifePermissions.VerifyOccurrence, organizationId, ct))
                        return TypedResults.Forbid();

                    var validation = new SightingReviewDtoValidator().Validate(dto);
                    if (!validation.IsValid)
                        return TypedResults.ValidationProblem(validation.ToDictionary());

                    var reviewer = new RequestClaimToken(user);
                    var result = await repository.ApproveAsync(
                        organizationId,
                        id,
                        reviewer.UserId!.Value,
                        reviewer.DisplayName,
                        string.IsNullOrWhiteSpace(dto.Reason) ? null : dto.Reason.Trim(),
                        ct
                    );
                    if (result.IsT1)
                        return TypedResults.NotFound();

                    return TypedResults.NoContent();
                }
            )
            .WithName("ApproveSighting");

        review
            .MapPost(
                "/{id:guid}/deny",
                async Task<Results<NoContent, NotFound, ForbidHttpResult, ValidationProblem>> (
                    Guid organizationId,
                    Guid id,
                    SightingReviewDto dto,
                    ClaimsPrincipal user,
                    IAuthorization auth,
                    ISightingRepository repository,
                    CancellationToken ct
                ) =>
                {
                    if (!await auth.HasAsync(WildlifePermissions.VerifyOccurrence, organizationId, ct))
                        return TypedResults.Forbid();

                    var validation = new SightingReviewDtoValidator().Validate(dto);
                    if (!validation.IsValid)
                        return TypedResults.ValidationProblem(validation.ToDictionary());

                    if (string.IsNullOrWhiteSpace(dto.Reason))
                        return TypedResults.ValidationProblem(
                            new Dictionary<string, string[]>
                            {
                                [nameof(SightingReviewDto.Reason)] = ["A reason is required to deny a sighting"],
                            }
                        );

                    var reviewer = new RequestClaimToken(user);
                    var result = await repository.DenyAsync(
                        organizationId,
                        id,
                        reviewer.UserId!.Value,
                        reviewer.DisplayName,
                        dto.Reason.Trim(),
                        ct
                    );
                    if (result.IsT1)
                        return TypedResults.NotFound();

                    return TypedResults.NoContent();
                }
            )
            .WithName("DenySighting");

        review
            .MapPost(
                "/{id:guid}/unapprove",
                async Task<Results<NoContent, NotFound, ForbidHttpResult>> (
                    Guid organizationId,
                    Guid id,
                    IAuthorization auth,
                    ISightingRepository repository,
                    CancellationToken ct
                ) =>
                {
                    if (!await auth.HasAsync(WildlifePermissions.VerifyOccurrence, organizationId, ct))
                        return TypedResults.Forbid();

                    var result = await repository.UnapproveAsync(organizationId, id, ct);
                    if (result.IsT1)
                        return TypedResults.NotFound();

                    return TypedResults.NoContent();
                }
            )
            .WithName("UnapproveSighting");

        return app;
    }
}
