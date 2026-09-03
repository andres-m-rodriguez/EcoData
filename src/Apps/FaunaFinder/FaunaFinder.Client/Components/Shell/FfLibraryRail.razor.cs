using System.Globalization;
using EcoData.Spa.Blazor;
using EcoData.Spa.Navigation;
using FaunaFinder.Client.Localization;
using FaunaFinder.Client.Services.Shapes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FaunaFinder.Client.Components.Shell;

public partial class FfLibraryRail : EcoDataComponent
{
    private const double DefaultRadiusKm = 5;

    private const long MaxShapeBytes = 10 * 1024 * 1024;

    [CascadingParameter]
    public LocaleContext Locale { get; set; } = LocaleContext.English;

    [Inject]
    private INavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ShapeAreaRequest Shapes { get; set; } = default!;

    private double? _latitude;
    private double? _longitude;
    private double _radiusKm = DefaultRadiusKm;
    private string? _coordinatesError;

    private bool _readingShape;
    private string? _shapeError;

    // The map page owns the search; the rail only hands it the point through the
    // URL, so the same link works from any page and can be shared.
    private void ShowOnMap()
    {
        if (_latitude is not { } latitude || _longitude is not { } longitude)
            return;

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            _coordinatesError = L["Rail_Coordinates_Invalid"];
            return;
        }

        _coordinatesError = null;
        var radiusKm = Math.Clamp(_radiusKm, 1, 50);
        var lat = latitude.ToString("F6", CultureInfo.InvariantCulture);
        var lng = longitude.ToString("F6", CultureInfo.InvariantCulture);
        var km = radiusKm.ToString("0.##", CultureInfo.InvariantCulture);
        Navigation.NavigateTo($"/?lat={lat}&lng={lng}&km={km}");
    }

    // Parsing runs here in the browser; the map only ever receives coordinate lists.
    private async Task LoadShapeAsync(InputFileChangeEventArgs e)
    {
        var file = e.File;
        _shapeError = null;

        if (file.Size > MaxShapeBytes)
        {
            _shapeError = L["Rail_Shape_Error_TooLarge"];
            return;
        }

        _readingShape = true;
        try
        {
            await using var stream = file.OpenReadStream(MaxShapeBytes);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);

            var content = buffer.ToArray();
            var result = ShapeFileReader.Read(file.Name, content);
            if (!result.TryPickT0(out var area, out var failed))
            {
                _shapeError = failed.Reason switch
                {
                    ShapeReadFailure.NoPolygon => L["Rail_Shape_Error_NoPolygon", file.Name],
                    ShapeReadFailure.NotWgs84 => L["Rail_Shape_Error_NotWgs84"],
                    _ => L["Rail_Shape_Error_Unreadable", file.Name],
                };
                return;
            }

            Shapes.Pending = area;
            Navigation.NavigateTo($"/?shape={Uri.EscapeDataString(area.Name)}");
        }
        finally
        {
            _readingShape = false;
        }
    }
}
