using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.DataAccess.Interfaces;
using EcoData.Identity.Contracts.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EcoData.AquaTrack.Api;

public static class ApiKeyEndpoints
{
    public static IEndpointRouteBuilder MapApiKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId:guid}/api-keys")
            .WithTags("API Keys")
            .RequireAuthorization(PolicyNames.Admin);

        group
            .MapGet(
                "/",
                async (
                    Guid organizationId,
                    IApiKeyRepository repository,
                    IOrganizationRepository organizationRepository,
                    CancellationToken ct
                ) =>
                {
                    var organization = await organizationRepository.GetByIdAsync(organizationId, ct);
                    if (organization is null)
                    {
                        return Results.NotFound("Organization not found");
                    }

                    var keys = await repository.GetByOrganizationAsync(organizationId, ct);
                    return Results.Ok(keys);
                }
            )
            .WithName("GetApiKeys");

        group
            .MapGet(
                "/{id:guid}",
                async (Guid organizationId, Guid id, IApiKeyRepository repository, CancellationToken ct) =>
                {
                    var key = await repository.GetByIdAsync(id, ct);
                    if (key is null || key.OrganizationId != organizationId)
                    {
                        return Results.NotFound();
                    }

                    return Results.Ok(key);
                }
            )
            .WithName("GetApiKeyById");

        group
            .MapPost(
                "/",
                async (
                    Guid organizationId,
                    ApiKeyDtoForCreate dto,
                    IApiKeyRepository repository,
                    IOrganizationRepository organizationRepository,
                    CancellationToken ct
                ) =>
                {
                    var organization = await organizationRepository.GetByIdAsync(organizationId, ct);
                    if (organization is null)
                    {
                        return Results.NotFound("Organization not found");
                    }

                    var created = await repository.CreateAsync(organizationId, dto, ct);
                    return Results.Created(
                        $"/api/organizations/{organizationId}/api-keys/{created.Id}",
                        created
                    );
                }
            )
            .WithName("CreateApiKey");

        group
            .MapDelete(
                "/{id:guid}",
                async (Guid organizationId, Guid id, IApiKeyRepository repository, CancellationToken ct) =>
                {
                    var key = await repository.GetByIdAsync(id, ct);
                    if (key is null || key.OrganizationId != organizationId)
                    {
                        return Results.NotFound();
                    }

                    var revoked = await repository.RevokeAsync(id, ct);
                    return revoked ? Results.NoContent() : Results.NotFound();
                }
            )
            .WithName("RevokeApiKey");

        return app;
    }
}
