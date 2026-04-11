namespace EcoData.NativeUi.Services;

/// <summary>
/// Action button displayed in the navbar (mobile app bar).
/// </summary>
public sealed record NavbarAction(string Title, Action OnClick);

/// <summary>
/// Manages the navbar state (title, action button) for native UI applications.
/// </summary>
public interface INativeNavbarManager
{
    /// <summary>
    /// Gets the current page title displayed in the navbar.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets the current action button displayed in the navbar (mobile only).
    /// </summary>
    NavbarAction? Action { get; }

    /// <summary>
    /// Sets the page title displayed in the navbar.
    /// </summary>
    void SetTitle(string? title);

    /// <summary>
    /// Sets the action button displayed in the navbar.
    /// </summary>
    void SetAction(string title, Action onClick);

    /// <summary>
    /// Clears the action button from the navbar.
    /// </summary>
    void ClearAction();

    /// <summary>
    /// Raised when the navbar state changes.
    /// </summary>
    event Action? OnStateChanged;
}

/// <summary>
/// Implementation of <see cref="INativeNavbarManager"/>.
/// </summary>
public sealed class NativeNavbarManager : INativeNavbarManager
{
    private string? _title;
    private NavbarAction? _action;

    public string? Title => _title;
    public NavbarAction? Action => _action;

    public event Action? OnStateChanged;

    public void SetTitle(string? title)
    {
        if (_title == title) return;
        _title = title;
        OnStateChanged?.Invoke();
    }

    public void SetAction(string title, Action onClick)
    {
        if (_action?.Title == title) return;
        _action = new NavbarAction(title, onClick);
        OnStateChanged?.Invoke();
    }

    public void ClearAction()
    {
        if (_action is null) return;
        _action = null;
        OnStateChanged?.Invoke();
    }
}
