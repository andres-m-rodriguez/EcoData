using EcoData.Identity.Contracts.Authorization;
using EcoData.Organization.Contracts.Dtos;
using EcoData.Organization.DataAccess.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace EcoData.Organization.Api;

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
                async Task<Results<Ok<IReadOnlyList<ApiKeyDtoForList>>, NotFound<string>>> (
                    Guid organizationId,
                    IApiKeyRepository repository,
                    IOrganizationRepository organizationRepository,
                    CancellationToken ct
                ) =>
                {
                    var organization = await organizationRepository.GetByIdAsync(
                        organizationId,
                        ct
                    );
                    if (organization is null)
                    {
                        return TypedResults.NotFound("Organization not found");
                    }

                    var keys = await repository.GetByOrganizationAsync(organizationId, ct);
                    return TypedResults.Ok(keys);
                }
            )
            .WithName("GetApiKeys");

        group
            .MapGet(
                "/{id:guid}",
                async Task<Results<Ok<ApiKeyDtoForDetail>, NotFound>> (
                    Guid organizationId,
                    Guid id,
                    IApiKeyRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var key = await repository.GetByIdAsync(id, ct);
                    if (key is null || key.OrganizationId != organizationId)
                    {
                        return TypedResults.NotFound();
                    }

                    return TypedResults.Ok(key);
                }
            )
            .WithName("GetApiKeyById");

        group
            .MapPost(
                "/",
                async Task<Results<Created<ApiKeyDtoForCreated>, NotFound<string>>> (
                    Guid organizationId,
                    ApiKeyDtoForCreate dto,
                    IApiKeyRepository repository,
                    IOrganizationRepository organizationRepository,
                    CancellationToken ct
                ) =>
                {
                    var organization = await organizationRepository.GetByIdAsync(
                        organizationId,
                        ct
                    );
                    if (organization is null)
                    {
                        return TypedResults.NotFound("Organization not found");
                    }

                    var created = await repository.CreateAsync(organizationId, dto, ct);
                    return TypedResults.Created(
                        $"/api/organizations/{organizationId}/api-keys/{created.Id}",
                        created
                    );
                }
            )
            .WithName("CreateApiKey");

        group
            .MapDelete(
                "/{id:guid}",
                async Task<Results<NoContent, NotFound>> (
                    Guid organizationId,
                    Guid id,
                    IApiKeyRepository repository,
                    CancellationToken ct
                ) =>
                {
                    var key = await repository.GetByIdAsync(id, ct);
                    if (key is null || key.OrganizationId != organizationId)
                    {
                        return TypedResults.NotFound();
                    }

                    var revoked = await repository.RevokeAsync(id, ct);
                    return revoked ? TypedResults.NoContent() : TypedResults.NotFound();
                }
            )
            .WithName("RevokeApiKey");

        return app;
    }
}
