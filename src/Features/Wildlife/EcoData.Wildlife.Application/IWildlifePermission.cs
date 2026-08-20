namespace EcoData.Wildlife.Application;

// The organization is always explicit: Wildlife checks against the org an operation
// targets and has no notion of a "current" one. Hosts that fix the org supply it.
public interface IWildlifePermission
{
    Task<bool> CanReadSpeciesAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<bool> CanSubmitOccurrenceAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task<bool> CanVerifyOccurrenceAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );
}
