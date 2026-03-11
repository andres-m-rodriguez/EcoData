using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace EcoData.Identity.Contracts.Claims;

public readonly record struct RequestClaimToken
{
    public const string OrganizationIdClaimType = "OrganizationId";

    public RequestClaimToken(IEnumerable<Claim> claims)
    {
        Guid? userId = null;
        Guid? organizationId = null;
        var displayName = string.Empty;
        var email = string.Empty;
        var role = string.Empty;

        foreach (var claim in claims)
        {
            switch (claim.Type)
            {
                case ClaimTypes.NameIdentifier:
                    userId = Guid.Parse(claim.Value);
                    break;
                case ClaimTypes.Name:
                    displayName = claim.Value;
                    break;
                case ClaimTypes.Email:
                    email = claim.Value;
                    break;
                case ClaimTypes.Role:
                    role = claim.Value;
                    break;
                case OrganizationIdClaimType:
                    if (Guid.TryParse(claim.Value, out var orgId))
                        organizationId = orgId;
                    break;
            }
        }

        UserId = userId;
        OrganizationId = organizationId;
        DisplayName = displayName;
        Email = email;
        Role = role;
    }

    public RequestClaimToken(ClaimsPrincipal principal)
        : this(principal.Claims) { }

    public Guid? UserId { get; }
    public Guid? OrganizationId { get; }
    public string DisplayName { get; }
    public string Email { get; }
    public string Role { get; }

    [MemberNotNullWhen(true, nameof(UserId))]
    public bool IsAuthenticated => UserId is not null;
}
