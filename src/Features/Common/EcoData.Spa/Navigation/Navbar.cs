using Tempest;

namespace EcoData.Spa.Navigation;


public sealed class Navbar(IEventBus bus)
{
    public string? Title { get; private set; }
    public IReadOnlyList<NavbarAction> Actions { get; private set; } = [];
    public void SetTitle(string? title)
    {
        if (Title == title)
            return;

        Title = title;
        bus.Publish<NavbarChanged>();
    }
    public void SetActions(params NavbarAction[] actions)
    {
        Actions = actions;
        bus.Publish<NavbarChanged>();
    }
}
