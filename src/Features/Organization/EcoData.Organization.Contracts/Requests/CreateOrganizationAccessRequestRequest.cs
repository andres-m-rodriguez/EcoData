namespace EcoData.Organization.Contracts.Requests;

public sealed record CreateOrganizationAccessRequestRequest(Guid OrganizationId, string? RequestMessage = null);
