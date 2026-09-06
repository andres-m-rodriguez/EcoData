using Microsoft.AspNetCore.Components;

namespace EcoPortal.Client.Services;

public enum NavigationTab
{
    Home,
    Data,
    Monitor,
    Orgs,
    Account
}

public interface ITabNavigationService
{
    NavigationTab CurrentTab { get; }

    void NavigateToTab(NavigationTab tab);
}

public sealed class TabNavigationService : ITabNavigationService
{
    private readonly NavigationManager _nav;

    public TabNavigationService(NavigationManager nav) => _nav = nav;

    public NavigationTab CurrentTab => GetTabFromPath(new Uri(_nav.Uri).AbsolutePath);

    public void NavigateToTab(NavigationTab tab) => _nav.NavigateTo(GetTabRoot(tab));

    private static string GetTabRoot(NavigationTab tab) => tab switch
    {
        NavigationTab.Home => "/",
        NavigationTab.Data => "/data",
        NavigationTab.Monitor => "/monitor",
        NavigationTab.Orgs => "/orgs",
        NavigationTab.Account => "/account",
        _ => "/"
    };

    private static NavigationTab GetTabFromPath(string path) => path switch
    {
        "/" or "" => NavigationTab.Home,
        _ when path.StartsWith("/data") => NavigationTab.Data,
        _ when path.StartsWith("/monitor") || path.StartsWith("/sensor") || path.StartsWith("/alerts")
            => NavigationTab.Monitor,
        _ when path.StartsWith("/orgs") || path.StartsWith("/organizations") || path.StartsWith("/access-requests")
            => NavigationTab.Orgs,
        _ when path.StartsWith("/account") || path.StartsWith("/login") || path.StartsWith("/register")
            => NavigationTab.Account,
        _ => NavigationTab.Home
    };
}
