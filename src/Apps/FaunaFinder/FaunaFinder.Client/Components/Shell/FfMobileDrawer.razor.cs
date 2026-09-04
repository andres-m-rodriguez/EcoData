using EcoData.Spa.Blazor;
using EcoData.Spa.Navigation.Events;
using FaunaFinder.Client.Layout;
using Tempest;

namespace FaunaFinder.Client.Components.Shell;

// The [Event] handlers live in this code-behind (with the base stated
// explicitly) because Tempest's razor frontend matches the @inherits text by
// simple name and can't see that EcoDataComponent is a StatefulComponent;
// the C# symbol frontend can.
public partial class FfMobileDrawer : EcoDataComponent
{
    // Published by the app bar's panel glyph.
    public sealed record Toggle;

    private bool _open;
    private DrawerTool _tool = DrawerTool.None;

    [Event]
    private void OnToggle(Toggle _)
    {
        if (_open)
        {
            Close();
            return;
        }

        if (_tool != DrawerTool.None)
        {
            CloseTool();
            return;
        }

        _open = true;
        Bus.Publish<MainLayout.TopBarHidden>();
    }

    // A tool that lands somewhere (coordinates, a shape) has done its job; the
    // map wants the screen back.
    [Event]
    private void OnNavigationChanged(NavigationChanged _)
    {
        if (_open)
            Close();
        else if (_tool != DrawerTool.None)
            CloseTool();
    }

    private string ToolTitle => _tool switch
    {
        DrawerTool.Coordinates => L["Rail_Card_Coordinates"],
        DrawerTool.Settings => L["Drawer_Settings_Heading"],
        _ => string.Empty,
    };

    // The drawer goes away but the top bar stays hidden: the tool takes its place.
    private void ShowTool(DrawerTool tool)
    {
        _open = false;
        _tool = tool;
    }

    // The top bar is already hidden, so only the panels swap.
    private void BackToDrawer()
    {
        _tool = DrawerTool.None;
        _open = true;
    }

    private void Close()
    {
        _open = false;
        Bus.Publish<MainLayout.TopBarShown>();
    }

    // The backdrop sits behind whichever panel is out.
    private void DismissAll()
    {
        if (_tool != DrawerTool.None)
        {
            CloseTool();
            return;
        }

        Close();
    }

    private void CloseTool()
    {
        _tool = DrawerTool.None;
        Bus.Publish<MainLayout.TopBarShown>();
    }

    private enum DrawerTool
    {
        None,
        Coordinates,
        Settings
    }
}
