using EcoData.NativeUi.Services;

namespace EcoPortal.Client.Services;

/// <summary>
/// Page action for the mobile app bar.
/// </summary>
public record PageAction(string Title, Action OnClick);

/// <summary>
/// Navigation service interface for EcoPortal.
/// Combines native navigation with app-specific tab and navbar features.
/// </summary>
public interface INavigationService
{
    // Core navigation state (delegates to INativeNavigationManager)
    string CurrentUri { get; }
    string CurrentPath { get; }
    bool CanGoBack { get; }
    NavigationDirection Direction { get; }

    // Tab navigation (delegates to ITabNavigationService)
    NavigationTab CurrentTab { get; }
    void NavigateToTab(NavigationTab tab);

    // Core navigation (delegates to INativeNavigationManager)
    void NavigateTo(string uri);
    void NavigateWithReplace(string uri);
    Task GoBackAsync();
    void GoBack();
    void SetParentPath(string parentPath);

    // Page navbar state (will move to INativeNavbarManager in future)
    string? PageTitle { get; }
    PageAction? PageAction { get; }
    void SetPageTitle(string? title);
    void SetPageAction(string title, Action onClick);
    void ClearPageAction();

    event Action? OnStateChanged;
}

/// <summary>
/// Implementation of <see cref="INavigationService"/> that delegates to native navigation
/// and tab navigation services.
/// </summary>
public sealed class NavigationService : INavigationService, IDisposable
{
    private readonly INativeNavigationManager _nav;
    private readonly ITabNavigationService _tabs;

    private string? _pageTitle;
    private PageAction? _pageAction;

    public NavigationService(INativeNavigationManager nav, ITabNavigationService tabs)
    {
        _nav = nav;
        _tabs = tabs;
        _nav.OnStateChanged += HandleNavigationStateChanged;
    }

    // Core navigation state
    public string CurrentUri => _nav.State.Uri;
    public string CurrentPath => _nav.State.Path;
    public bool CanGoBack => _nav.State.CanGoBack;
    public NavigationDirection Direction => _nav.State.Direction;

    // Tab navigation
    public NavigationTab CurrentTab => _tabs.CurrentTab;
    public void NavigateToTab(NavigationTab tab) => _tabs.NavigateToTab(tab);

    // Core navigation
    public void NavigateTo(string uri) => _nav.NavigateTo(uri);
    public void NavigateWithReplace(string uri) => _nav.NavigateTo(uri, replace: true);
    public Task GoBackAsync() => _nav.GoBackAsync();
    public void GoBack() => _ = _nav.GoBackAsync();
    public void SetParentPath(string parentPath) => _nav.SetParentPath(parentPath);

    // Page navbar state
    public string? PageTitle => _pageTitle;
    public PageAction? PageAction => _pageAction;

    public void SetPageTitle(string? title)
    {
        if (_pageTitle == title) return;
        _pageTitle = title;
        OnStateChanged?.Invoke();
    }

    public void SetPageAction(string title, Action onClick)
    {
        if (_pageAction?.Title == title) return;
        _pageAction = new PageAction(title, onClick);
        OnStateChanged?.Invoke();
    }

    public void ClearPageAction()
    {
        if (_pageAction is null) return;
        _pageAction = null;
        OnStateChanged?.Invoke();
    }

    public event Action? OnStateChanged;

    private void HandleNavigationStateChanged()
    {
        OnStateChanged?.Invoke();
    }

    public void Dispose()
    {
        _nav.OnStateChanged -= HandleNavigationStateChanged;
    }
}
