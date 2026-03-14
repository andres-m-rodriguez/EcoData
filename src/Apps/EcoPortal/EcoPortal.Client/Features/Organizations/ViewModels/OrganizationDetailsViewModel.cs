using EcoData.Organization.Contracts.Dtos;
using EcoData.Sensors.Contracts.Dtos;
using MudBlazor;

namespace EcoPortal.Client.Features.Organizations.ViewModels;

public class OrganizationDetailsViewModel
{
    private readonly OrganizationDtoForDetail _organization;
    private readonly List<SensorDtoForList> _sensors;
    private readonly List<OrganizationMemberDto> _members;
    private readonly int _totalSensorCount;

    public OrganizationDetailsViewModel(
        OrganizationDtoForDetail organization,
        List<SensorDtoForList> sensors,
        List<OrganizationMemberDto> members,
        int totalSensorCount
    )
    {
        _organization = organization;
        _sensors = sensors;
        _members = members;
        _totalSensorCount = totalSensorCount;
    }

    // Organization Info
    public Guid Id => _organization.Id;
    public string Name => _organization.Name;
    public string? ProfilePictureUrl => _organization.ProfilePictureUrl;
    public string? CardPictureUrl => _organization.CardPictureUrl;
    public string? AboutUs => _organization.AboutUs;
    public string? WebsiteUrl => _organization.WebsiteUrl;

    // Display Properties
    public bool HasProfilePicture => !string.IsNullOrWhiteSpace(ProfilePictureUrl);
    public bool HasCardPicture => !string.IsNullOrWhiteSpace(CardPictureUrl);
    public bool HasWebsite => !string.IsNullOrWhiteSpace(WebsiteUrl);
    public bool HasAbout => !string.IsNullOrWhiteSpace(AboutUs);

    public string DisplayWebsiteUrl =>
        Uri.TryCreate(WebsiteUrl, UriKind.Absolute, out var uri)
            ? uri.Host
            : WebsiteUrl ?? string.Empty;

    public string FormattedCreatedDate => _organization.CreatedAt.ToString("MMM d, yyyy");

    public string PageTitle => $"{Name} - EcoData";

    // Sensor Stats
    public int TotalSensorCount => _totalSensorCount;
    public int OnlineSensorCount => _sensors.Count(s => s.IsActive);
    public bool HasSensors => _totalSensorCount > 0;
    public bool ShowViewAllSensors => _totalSensorCount > MaxDisplayedSensors;
    public IEnumerable<SensorDtoForList> DisplayedSensors => _sensors;

    public string SensorCountBadge => _totalSensorCount.ToString();
    public string OnlineSensorSubtitle => $"{OnlineSensorCount} online now";
    public string ViewAllSensorsText => $"View all {_totalSensorCount} sensors";

    // Member Stats
    public int MemberCount => _members.Count;
    public int AdminCount => _members.Count(m => m.RoleName == "Admin");
    public bool HasMembers => _members.Count > 0;
    public bool ShowViewAllMembers => _members.Count > MaxDisplayedMembers;
    public IEnumerable<OrganizationMemberDto> DisplayedMembers =>
        _members.Take(MaxDisplayedMembers);

    public string MemberCountBadge => _members.Count.ToString();
    public string AdminCountSubtitle => $"{AdminCount} admins";
    public string ViewAllMembersText => $"View all {_members.Count} members";

    // Configuration
    public const int MaxDisplayedSensors = 6;
    public const int MaxDisplayedMembers = 10;

    // Member Display Helpers
    public static char GetMemberInitial(string displayName) =>
        string.IsNullOrWhiteSpace(displayName) ? '?' : char.ToUpper(displayName[0]);

    public static Color GetMemberAvatarColor(string displayName)
    {
        var colors = new[]
        {
            Color.Primary,
            Color.Secondary,
            Color.Tertiary,
            Color.Info,
            Color.Success,
            Color.Warning,
        };
        var hash = displayName.GetHashCode();
        return colors[Math.Abs(hash) % colors.Length];
    }
}
