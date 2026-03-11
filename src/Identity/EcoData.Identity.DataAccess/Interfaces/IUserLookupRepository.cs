namespace EcoData.Identity.DataAccess.Interfaces;

public record UserLookupDto(Guid Id, string Email, string DisplayName);

public interface IUserLookupRepository
{
    Task<IReadOnlyDictionary<Guid, UserLookupDto>> GetByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default
    );

    Task<UserLookupDto?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<bool> ExistsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
}
