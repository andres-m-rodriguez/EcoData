using EcoData.Spa.Contracts.Events;
using EcoData.Spa.Contracts.Navbar;
using Tempest;

namespace EcoData.Spa.Core.Navbar;

/// <summary>
/// Implementation of <see cref="INavbarManager"/>.
/// </summary>
internal sealed class SpaNavbarManager(IEventBus bus) : INavbarManager
{
    private static readonly NavbarState DefaultState = new(null, []);

    private NavbarState _state = DefaultState;

    public NavbarState State => _state;

    public void SetState(NavbarState state)
    {
        if (_state == state)
            return;
        _state = state;
        Notify();
    }

    public void SetTitle(string? title)
    {
        if (_state.Title == title)
            return;
        _state = _state with { Title = title };
        Notify();
    }

    public void SetActions(params NavbarAction[] actions)
    {
        _state = _state with { Actions = actions };
        Notify();
    }

    public void ClearActions()
    {
        if (_state.Actions.Count == 0)
            return;
        _state = _state with { Actions = [] };
        Notify();
    }

    public void Reset()
    {
        if (_state == DefaultState)
            return;
        _state = DefaultState;
        Notify();
    }

    private void Notify() => bus.Publish(new NavbarChanged(_state));
}
