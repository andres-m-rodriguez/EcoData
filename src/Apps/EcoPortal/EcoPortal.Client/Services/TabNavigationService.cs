using EcoData.NativeUi.Services;

namespace EcoPortal.Client.Services;

/// <summary>
/// App-specific tab navigation enum.
/// </summary>
public enum NavigationTab
{
    Home,
    Monitor,
    Orgs,
    Account
}

/// <summary>
/// Service for tab-based navigation in the EcoPortal app.
/// </summary>
public interface ITabNavigationService
{
    /// <summary>
    /// Gets the currently active tab based on the current path.
    /// </summary>
    NavigationTab CurrentTab { get; }

    /// <summary>
    /// Navigates to the root page of the specified tab.
    /// </summary>
    void NavigateToTab(NavigationTab tab);
}

/// <summary>
/// Implementation of <see cref="ITabNavigationService"/> that uses the native navigation manager.
/// </summary>
public sealed class TabNavigationService : ITabNavigationService
{
    private readonly INativeNavigationManager _nav;

    public TabNavigationService(INativeNavigationManager nav) => _nav = nav;

    public NavigationTab CurrentTab => GetTabFromPath(_nav.State.Path);

    public void NavigateToTab(NavigationTab tab) => _nav.NavigateTo(GetTabRoot(tab));

    private static string GetTabRoot(NavigationTab tab) => tab switch
    {
        NavigationTab.Home => "/",
        NavigationTab.Monitor => "/monitor",
        NavigationTab.Orgs => "/orgs",
        NavigationTab.Account => "/account",
        _ => "/"
    };

    private static NavigationTab GetTabFromPath(string path) => path switch
    {
        "/" or "" => NavigationTab.Home,
        _ when path.StartsWith("/monitor") || path.StartsWith("/sensors") || path.StartsWith("/alerts")
            => NavigationTab.Monitor,
        _ when path.StartsWith("/orgs") || path.StartsWith("/organizations") || path.StartsWith("/access-requests")
            => NavigationTab.Orgs,
        _ when path.StartsWith("/account") || path.StartsWith("/login") || path.StartsWith("/register")
            => NavigationTab.Account,
        _ => NavigationTab.Home
    };
}
