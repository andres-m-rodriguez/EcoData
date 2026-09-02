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
    private const int MaxImagesPerSighting = 5;
    private const long MaxImageBytes = 10 * 1024 * 1024;

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
                    SightingApprovalDto dto,
                    ClaimsPrincipal user,
                    IAuthorization auth,
                    ISightingRepository repository,
                    CancellationToken ct
                ) =>
                {
                    if (!await auth.HasAsync(WildlifePermissions.VerifyOccurrence, organizationId, ct))
                        return TypedResults.Forbid();

                    var validation = new SightingApprovalDtoValidator().Validate(dto);
                    if (!validation.IsValid)
                        return TypedResults.ValidationProblem(validation.ToDictionary());

                    var reviewer = new RequestClaimToken(user);
                    var result = await repository.ApproveAsync(
                        organizationId,
                        id,
                        reviewer.UserId!.Value,
                        reviewer.DisplayName,
                        dto.Reason,
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
                    SightingDenialDto dto,
                    ClaimsPrincipal user,
                    IAuthorization auth,
                    ISightingRepository repository,
                    CancellationToken ct
                ) =>
                {
                    if (!await auth.HasAsync(WildlifePermissions.VerifyOccurrence, organizationId, ct))
                        return TypedResults.Forbid();

                    var validation = new SightingDenialDtoValidator().Validate(dto);
                    if (!validation.IsValid)
                        return TypedResults.ValidationProblem(validation.ToDictionary());

                    var reviewer = new RequestClaimToken(user);
                    var result = await repository.DenyAsync(
                        organizationId,
                        id,
                        reviewer.UserId!.Value,
                        reviewer.DisplayName,
                        dto.Reason,
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

        // Photos: the reporter attaches and removes them, the reporter or a
        // reviewer in the organization reads them. The container is private, so
        // every read streams through here after the same ownership check.
        var images = app.MapGroup("/wildlife/sightings/{id:guid}/images")
            .WithTags("Sightings")
            .RequireAuthorization();

        images
            .MapPost(
                "/",
                async Task<
                    Results<Created<SightingImageDto>, ValidationProblem, NotFound, ForbidHttpResult>
                > (
                    Guid id,
                    IFormFile file,
                    ClaimsPrincipal user,
                    ISightingRepository repository,
                    ISightingImageStore store,
                    CancellationToken ct
                ) =>
                {
                    var owner = await repository.GetOwnerAsync(id, ct);
                    if (owner is null)
                        return TypedResults.NotFound();

                    var caller = new RequestClaimToken(user);
                    if (caller.UserId != owner.Value.ReporterUserId)
                        return TypedResults.Forbid();

                    if (await repository.CountImagesAsync(id, ct) >= MaxImagesPerSighting)
                        return FileProblem($"A sighting can have at most {MaxImagesPerSighting} images");

                    if (file.Length == 0)
                        return FileProblem("The file is empty");

                    if (file.Length > MaxImageBytes)
                        return FileProblem("Images must be 10 MB or less");

                    // The declared type is whatever the browser guessed from the
                    // file name; the first bytes say what the file really is.
                    await using var content = file.OpenReadStream();
                    var header = new byte[12];
                    await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
                    var contentType = header switch
                    {
                        [0xFF, 0xD8, 0xFF, ..] => "image/jpeg",
                        [0x89, 0x50, 0x4E, 0x47, ..] => "image/png",
                        [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50] => "image/webp",
                        _ => null,
                    };
                    if (contentType is null)
                        return FileProblem("Images must be JPEG, PNG or WebP");
                    content.Position = 0;

                    var imageId = Guid.CreateVersion7();
                    var extension = contentType switch
                    {
                        "image/jpeg" => "jpg",
                        "image/png" => "png",
                        _ => "webp",
                    };
                    var blobName = $"{id}/{imageId}.{extension}";

                    // Blob first: a failed upload leaves no row pointing at nothing.
                    await store.UploadAsync(blobName, contentType, content, ct);
                    var image = await repository.AddImageAsync(
                        id,
                        imageId,
                        caller.UserId!.Value,
                        caller.DisplayName,
                        blobName,
                        contentType,
                        file.Length,
                        ct
                    );

                    return TypedResults.Created($"/wildlife/sightings/{id}/images/{image.Id}", image);
                }
            )
            // A minimal-API IFormFile parameter demands an antiforgery token; the
            // session cookie is SameSite=Strict, so a cross-site form post never
            // carries it and the token would guard nothing.
            .DisableAntiforgery()
            .WithName("UploadSightingImage");

        images
            .MapGet(
                "/{imageId:guid}",
                async Task<Results<FileStreamHttpResult, NotFound, ForbidHttpResult>> (
                    Guid id,
                    Guid imageId,
                    ClaimsPrincipal user,
                    HttpResponse response,
                    IAuthorization auth,
                    ISightingRepository repository,
                    ISightingImageStore store,
                    CancellationToken ct
                ) =>
                {
                    var owner = await repository.GetOwnerAsync(id, ct);
                    if (owner is null)
                        return TypedResults.NotFound();

                    var caller = new RequestClaimToken(user);
                    if (
                        caller.UserId != owner.Value.ReporterUserId
                        && !await auth.HasAsync(
                            WildlifePermissions.VerifyOccurrence,
                            owner.Value.OrganizationId,
                            ct
                        )
                    )
                        return TypedResults.Forbid();

                    var image = await repository.GetImageAsync(id, imageId, ct);
                    if (image is null)
                        return TypedResults.NotFound();

                    var content = await store.OpenReadAsync(image.BlobName, ct);
                    if (content is null)
                        return TypedResults.NotFound();

                    // Reusable within a session, never shareable: the cookie
                    // decides who may read, so the cache stays private.
                    response.Headers.CacheControl = "private, max-age=3600";
                    return TypedResults.Stream(content, image.ContentType);
                }
            )
            .WithName("GetSightingImage");

        images
            .MapDelete(
                "/{imageId:guid}",
                async Task<Results<NoContent, NotFound, ForbidHttpResult>> (
                    Guid id,
                    Guid imageId,
                    ClaimsPrincipal user,
                    ISightingRepository repository,
                    ISightingImageStore store,
                    CancellationToken ct
                ) =>
                {
                    var owner = await repository.GetOwnerAsync(id, ct);
                    if (owner is null)
                        return TypedResults.NotFound();

                    var caller = new RequestClaimToken(user);
                    if (caller.UserId != owner.Value.ReporterUserId)
                        return TypedResults.Forbid();

                    var image = await repository.GetImageAsync(id, imageId, ct);
                    if (image is null)
                        return TypedResults.NotFound();

                    // Row first, so a blob delete that fails leaves nothing the
                    // API would still serve; the store tolerates a blob already gone.
                    var deleted = await repository.DeleteImageAsync(id, imageId, ct);
                    if (deleted.IsT1)
                        return TypedResults.NotFound();

                    await store.DeleteAsync(image.BlobName, ct);
                    return TypedResults.NoContent();
                }
            )
            .WithName("DeleteSightingImage");

        return app;
    }

    private static ValidationProblem FileProblem(string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["File"] = [message] });
}
