using EcoData.Spa.Blazor;

namespace FaunaFinder.Client.Components.Shell;

public partial class FfLibraryRail : EcoDataComponent
{
    private RailView _view = RailView.Cards;

    private void ShowCards() => _view = RailView.Cards;

    private void ShowCoordinates() => _view = RailView.Coordinates;

    private enum RailView
    {
        Cards,
        Coordinates
    }
}
