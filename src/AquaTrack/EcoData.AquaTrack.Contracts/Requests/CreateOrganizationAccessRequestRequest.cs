namespace EcoData.AquaTrack.Contracts.Requests;

public sealed record CreateOrganizationAccessRequestRequest(Guid OrganizationId, string? RequestMessage = null);
