namespace EcoData.Organization.Contracts.Requests;

// Shared by create and update: a role is its name plus the permission keys it grants.
public sealed record OrganizationRoleRequest(string Name, IReadOnlyList<string> Permissions);
