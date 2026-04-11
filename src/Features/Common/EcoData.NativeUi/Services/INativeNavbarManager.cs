namespace EcoData.NativeUi.Services;

/// <summary>
/// Action button displayed in the navbar (mobile app bar).
/// </summary>
public sealed record NavbarAction(string Title, Action OnClick);

/// <summary>
/// Represents the complete navbar state including title and actions.
/// </summary>
public sealed record NavbarState(string? Title, IReadOnlyList<NavbarAction> Actions);

/// <summary>
/// Manages the navbar state (title, actions) for native UI applications.
/// </summary>
public interface INativeNavbarManager
{
    /// <summary>
    /// Gets the current navbar state including title and actions.
    /// </summary>
    NavbarState State { get; }

    /// <summary>
    /// Sets the complete navbar state at once.
    /// </summary>
    void SetState(NavbarState state);

    /// <summary>
    /// Sets the page title displayed in the navbar.
    /// </summary>
    void SetTitle(string? title);

    /// <summary>
    /// Sets the action buttons displayed in the navbar.
    /// </summary>
    void SetActions(params NavbarAction[] actions);

    /// <summary>
    /// Clears all action buttons from the navbar.
    /// </summary>
    void ClearActions();

    /// <summary>
    /// Resets the navbar to its default state (no title, no actions).
    /// </summary>
    void Reset();

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
    private static readonly NavbarState DefaultState = new(null, []);

    private NavbarState _state = DefaultState;

    public NavbarState State => _state;

    public event Action? OnStateChanged;

    public void SetState(NavbarState state)
    {
        if (_state == state)
            return;
        _state = state;
        OnStateChanged?.Invoke();
    }

    public void SetTitle(string? title)
    {
        if (_state.Title == title)
            return;
        _state = _state with { Title = title };
        OnStateChanged?.Invoke();
    }

    public void SetActions(params NavbarAction[] actions)
    {
        _state = _state with { Actions = actions };
        OnStateChanged?.Invoke();
    }

    public void ClearActions()
    {
        if (_state.Actions.Count == 0)
            return;
        _state = _state with { Actions = [] };
        OnStateChanged?.Invoke();
    }

    public void Reset()
    {
        if (_state == DefaultState)
            return;
        _state = DefaultState;
        OnStateChanged?.Invoke();
    }
}
