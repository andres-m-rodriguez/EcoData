using EcoData.Identity.Contracts.Dtos;

namespace EcoData.Identity.Application.Server.Services;

public interface IUserLookupService
{
    Task<IReadOnlyDictionary<Guid, UserLookupDto>> GetByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default
    );

    Task<UserLookupDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsGlobalAdminAsync(Guid userId, CancellationToken cancellationToken = default);
}
