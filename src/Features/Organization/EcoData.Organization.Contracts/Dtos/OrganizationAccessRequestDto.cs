namespace EcoData.Organization.Contracts.Dtos;

public sealed record OrganizationAccessRequestDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserDisplayName,
    Guid OrganizationId,
    string OrganizationName,
    string Status,
    string? RequestMessage,
    string? ReviewNotes,
    Guid? ReviewedByUserId,
    string? ReviewedByDisplayName,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset CreatedAt
);
