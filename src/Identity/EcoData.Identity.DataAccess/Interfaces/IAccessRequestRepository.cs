using EcoData.Identity.Contracts.Parameters;
using EcoData.Identity.Contracts.Responses;
using EcoData.Identity.Database.Models;

namespace EcoData.Identity.DataAccess.Interfaces;

public interface IAccessRequestRepository
{
    Task<AccessRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccessRequest?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AccessRequest> CreateAsync(AccessRequest accessRequest, CancellationToken cancellationToken = default);
    Task<AccessRequest> UpdateAsync(AccessRequest accessRequest, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AccessRequestResponse> GetAccessRequestsAsync(AccessRequestParameters parameters, CancellationToken cancellationToken = default);
}
