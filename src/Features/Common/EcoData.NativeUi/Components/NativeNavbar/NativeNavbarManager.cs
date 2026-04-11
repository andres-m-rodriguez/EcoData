namespace EcoData.NativeUi.Components.NativeNavbar;

/// <summary>
/// Implementation of <see cref="INativeNavbarManager"/>.
/// </summary>
internal sealed class NativeNavbarManager : INativeNavbarManager
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
