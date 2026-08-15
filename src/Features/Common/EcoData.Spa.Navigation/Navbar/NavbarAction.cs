namespace EcoData.Spa.Navigation.Navbar;

/// <summary>
/// Action button displayed in the navbar (mobile app bar).
/// </summary>
public sealed record NavbarAction(string Title, Action OnClick);
