namespace EcoData.Organization.Contracts.Requests;

public sealed record OrganizationRoleRequest(string Name, IReadOnlyList<string> Permissions);
