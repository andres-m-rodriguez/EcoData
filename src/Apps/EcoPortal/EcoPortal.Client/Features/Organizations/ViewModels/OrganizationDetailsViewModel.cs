using EcoData.Organization.Contracts.Dtos;
using MudBlazor;

namespace EcoPortal.Client.Features.Organizations.ViewModels;

public class OrganizationDetailsViewModel(OrganizationDtoForDetail organization)
{
    // Organization Info
    public Guid Id => organization.Id;
    public string Name => organization.Name;
    public string Slug => organization.Slug;
    public string? Tagline => organization.Tagline;
    public string? ProfilePictureUrl => organization.ProfilePictureUrl;
    public string? CardPictureUrl => organization.CardPictureUrl;
    public string? AboutUs => organization.AboutUs;
    public string? WebsiteUrl => organization.WebsiteUrl;
    public string? Location => organization.Location;
    public int? FoundedYear => organization.FoundedYear;
    public string? LegalStatus => organization.LegalStatus;
    public string? TaxId => organization.TaxId;
    public string? PrimaryColor => organization.PrimaryColor;
    public string? AccentColor => organization.AccentColor;

    // Display Properties
    public bool HasProfilePicture => !string.IsNullOrWhiteSpace(ProfilePictureUrl);
    public bool HasCardPicture => !string.IsNullOrWhiteSpace(CardPictureUrl);
    public bool HasWebsite => !string.IsNullOrWhiteSpace(WebsiteUrl);
    public bool HasAbout => !string.IsNullOrWhiteSpace(AboutUs);
    public bool HasTagline => !string.IsNullOrWhiteSpace(Tagline);
    public bool HasLocation => !string.IsNullOrWhiteSpace(Location);
    public bool HasFoundedYear => FoundedYear.HasValue;
    public bool HasLegalInfo => !string.IsNullOrWhiteSpace(LegalStatus) || !string.IsNullOrWhiteSpace(TaxId);

    public string DisplayWebsiteUrl =>
        Uri.TryCreate(WebsiteUrl, UriKind.Absolute, out var uri)
            ? uri.Host
            : WebsiteUrl ?? string.Empty;

    public string FormattedCreatedDate => organization.CreatedAt.ToString("MMM d, yyyy");
    public int JoinedYear => organization.CreatedAt.Year;

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name)) return "?";
            var parts = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
            return string.Concat(parts[0][0], parts[1][0]).ToUpperInvariant();
        }
    }

    // Inline CSS variables that scope per-org branding to the page. Falls back
    // to MudBlazor palette tokens when the org hasn't picked colors yet, so
    // the layout reads correctly either way.
    public string ThemeStyle =>
        $"--org-primary: {PrimaryColor ?? "var(--mud-palette-primary)"}; " +
        $"--org-accent: {AccentColor ?? "var(--mud-palette-secondary)"};";

    public string LegalLine
    {
        get
        {
            var hasStatus = !string.IsNullOrWhiteSpace(LegalStatus);
            var hasTaxId = !string.IsNullOrWhiteSpace(TaxId);
            return (hasStatus, hasTaxId) switch
            {
                (true, true) => $"{LegalStatus} · {TaxId}",
                (true, false) => LegalStatus!,
                (false, true) => TaxId!,
                _ => string.Empty
            };
        }
    }

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
