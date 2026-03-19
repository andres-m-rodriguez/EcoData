using EcoData.Organization.Contracts.Dtos;
using MudBlazor;

namespace EcoPortal.Client.Features.Organizations.ViewModels;

public class OrganizationDetailsViewModel(OrganizationDtoForDetail organization)
{
    // Organization Info
    public Guid Id => organization.Id;
    public string Name => organization.Name;
    public string? ProfilePictureUrl => organization.ProfilePictureUrl;
    public string? CardPictureUrl => organization.CardPictureUrl;
    public string? AboutUs => organization.AboutUs;
    public string? WebsiteUrl => organization.WebsiteUrl;

    // Display Properties
    public bool HasProfilePicture => !string.IsNullOrWhiteSpace(ProfilePictureUrl);
    public bool HasCardPicture => !string.IsNullOrWhiteSpace(CardPictureUrl);
    public bool HasWebsite => !string.IsNullOrWhiteSpace(WebsiteUrl);
    public bool HasAbout => !string.IsNullOrWhiteSpace(AboutUs);

    public string DisplayWebsiteUrl =>
        Uri.TryCreate(WebsiteUrl, UriKind.Absolute, out var uri)
            ? uri.Host
            : WebsiteUrl ?? string.Empty;

    public string FormattedCreatedDate => organization.CreatedAt.ToString("MMM d, yyyy");

    public string PageTitle => $"{Name} - EcoData";

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
