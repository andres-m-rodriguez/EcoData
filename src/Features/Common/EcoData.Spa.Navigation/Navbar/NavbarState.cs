namespace EcoData.Spa.Navigation.Navbar;

/// <summary>
/// Represents the complete navbar state including title and actions.
/// </summary>
public sealed record NavbarState(string? Title, IReadOnlyList<NavbarAction> Actions);
