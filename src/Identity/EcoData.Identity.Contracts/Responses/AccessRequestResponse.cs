namespace EcoData.Identity.Contracts.Responses;

public sealed record AccessRequestResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string Status,
    string? ReviewNotes,
    Guid? ReviewedById,
    string? ReviewedByDisplayName,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset CreatedAt,
    Guid RequestedOrganizationId,
    string RequestedOrganizationName,
    Guid? CreatedUserId = null
);
