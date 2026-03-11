using EcoData.AquaTrack.Contracts.Errors;
using EcoData.Identity.Contracts.Parameters;
using EcoData.Identity.Contracts.Responses;
using OneOf;

namespace EcoData.AquaTrack.Application.Client;

public interface IAccessRequestHttpClient
{
    IAsyncEnumerable<AccessRequestResponse> GetAllAsync(
        Guid organizationId,
        AccessRequestParameters parameters,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<AccessRequestResponse, NotFoundError, ConflictError, ApiError>> UpdateStatusAsync(
        Guid organizationId,
        Guid accessRequestId,
        bool approved,
        string? reviewNotes = null,
        CancellationToken cancellationToken = default
    );
}
