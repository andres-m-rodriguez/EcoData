namespace FaunaFinder.Client.Services.Shapes;

// Hands a parsed shape from the rail to the map page. The rings are far too
// large for the URL, so the URL carries only the file name and the map takes
// the rings from here; a reload finds nothing pending and shows nothing.
public sealed class ShapeAreaRequest
{
    public ShapeArea? Pending { get; set; }
}
