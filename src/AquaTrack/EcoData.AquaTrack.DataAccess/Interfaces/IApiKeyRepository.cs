using EcoData.AquaTrack.Contracts.Dtos;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface IApiKeyRepository
{
    Task<IReadOnlyList<ApiKeyDtoForList>> GetByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<ApiKeyDtoForDetail?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<ApiKeyDtoForCreated> CreateAsync(
        Guid organizationId,
        ApiKeyDtoForCreate dto,
        CancellationToken cancellationToken = default
    );

    Task<bool> RevokeAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<ApiKeyValidationResult> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default
    );

    Task UpdateLastUsedAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );
}
