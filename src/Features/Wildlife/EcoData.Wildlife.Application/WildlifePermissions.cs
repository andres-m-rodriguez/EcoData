using EcoData.Common.Authorization;
using EcoData.Wildlife.Contracts;

namespace EcoData.Wildlife.Application;

public static class WildlifePermissions
{
    public static readonly OrgPermission ReadSpecies = new(Permissions.Species.Read);

    public static readonly OrgPermission SubmitOccurrence = new(Permissions.Occurrence.Submit);

    public static readonly OrgPermission VerifyOccurrence = new(Permissions.Occurrence.Verify);
}
