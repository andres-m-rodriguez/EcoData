using EcoData.Common.Authorization;

namespace EcoData.Wildlife.Application;

public static class WildlifePermissions
{
    public static readonly OrgPermission ReadSpecies = new("wildlife:species:read");

    public static readonly OrgPermission SubmitOccurrence = new("wildlife:occurrence:submit");

    public static readonly OrgPermission VerifyOccurrence = new("wildlife:occurrence:verify");
}
