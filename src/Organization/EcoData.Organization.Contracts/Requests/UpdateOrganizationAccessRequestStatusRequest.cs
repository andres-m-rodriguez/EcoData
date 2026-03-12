namespace EcoData.Organization.Contracts.Requests;

public sealed record UpdateOrganizationAccessRequestStatusRequest(bool Approved, string? ReviewNotes = null);
