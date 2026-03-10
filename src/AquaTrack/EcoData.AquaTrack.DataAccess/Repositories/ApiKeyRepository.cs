using System.Security.Cryptography;
using System.Text;
using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Database;
using EcoData.AquaTrack.Database.Models;
using EcoData.AquaTrack.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EcoData.AquaTrack.DataAccess.Repositories;

public sealed class ApiKeyRepository(IDbContextFactory<AquaTrackDbContext> contextFactory)
    : IApiKeyRepository
{
    private const int KeyLength = 32;
    private const int PrefixLength = 8;

    public async Task<IReadOnlyList<ApiKeyDtoForList>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ApiKeys
            .Where(k => k.OrganizationId == organizationId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyDtoForList(
                k.Id,
                k.KeyPrefix,
                k.Name,
                k.Scopes,
                k.ExpiresAt,
                k.LastUsedAt,
                k.IsActive,
                k.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiKeyDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ApiKeys
            .Where(k => k.Id == id)
            .Select(k => new ApiKeyDtoForDetail(
                k.Id,
                k.OrganizationId,
                k.KeyPrefix,
                k.Name,
                k.Scopes,
                k.ExpiresAt,
                k.LastUsedAt,
                k.IsActive,
                k.CreatedAt,
                k.RevokedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ApiKeyDtoForCreated> CreateAsync(
        Guid organizationId,
        ApiKeyDtoForCreate dto,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var plainTextKey = GenerateApiKey();
        var keyHash = HashKey(plainTextKey);
        var keyPrefix = plainTextKey[..PrefixLength];

        var entity = new ApiKey
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Name = dto.Name,
            Scopes = dto.Scopes,
            ExpiresAt = dto.ExpiresAt,
            LastUsedAt = null,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            RevokedAt = null,
        };

        context.ApiKeys.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return new ApiKeyDtoForCreated(entity.Id, keyPrefix, plainTextKey);
    }

    public async Task<bool> RevokeAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.ApiKeys.FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        context.ApiKeys.Attach(entity);
        entity.IsActive = false;
        entity.RevokedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ApiKeyValidationResult> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ApiKeyValidationResult(false, null, [], "API key is required");
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var keyHash = HashKey(apiKey);
        var now = DateTimeOffset.UtcNow;

        var entity = await context.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);

        if (entity is null)
        {
            return new ApiKeyValidationResult(false, null, [], "Invalid API key");
        }

        if (!entity.IsActive)
        {
            return new ApiKeyValidationResult(false, null, [], "API key has been revoked");
        }

        if (entity.ExpiresAt.HasValue && entity.ExpiresAt.Value < now)
        {
            return new ApiKeyValidationResult(false, null, [], "API key has expired");
        }

        return new ApiKeyValidationResult(true, entity.OrganizationId, entity.Scopes, null);
    }

    public async Task UpdateLastUsedAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = await context.ApiKeys.FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
        if (entity is not null)
        {
            context.ApiKeys.Attach(entity);
            entity.LastUsedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(KeyLength);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string HashKey(string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexStringLower(bytes);
    }
}
