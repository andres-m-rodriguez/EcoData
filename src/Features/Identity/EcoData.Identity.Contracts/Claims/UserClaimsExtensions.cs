using System.Security.Claims;
using EcoData.Identity.Contracts.Responses;

namespace EcoData.Identity.Contracts.Claims;

public static class UserClaimsExtensions
{
    public static IEnumerable<Claim> ToClaims(this UserInfo user)
    {
        yield return new Claim(ClaimTypes.NameIdentifier, user.Id.ToString());
        yield return new Claim(ClaimTypes.Email, user.Email);
        yield return new Claim(ClaimTypes.Name, user.DisplayName);

        if (user.GlobalRole.HasValue)
        {
            yield return new Claim(ClaimTypes.Role, user.GlobalRole.Value.ToString());
        }
    }

    public static ClaimsPrincipal ToClaimsPrincipal(this UserInfo? user, string authenticationType = "EcoPortal")
    {
        if (user is null)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var identity = new ClaimsIdentity(user.ToClaims(), authenticationType);
        return new ClaimsPrincipal(identity);
    }
}
