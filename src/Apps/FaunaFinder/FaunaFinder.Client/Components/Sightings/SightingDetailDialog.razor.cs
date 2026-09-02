using System.Globalization;
using EcoData.Spa.Blazor;
using EcoData.Spa.Map;
using EcoData.Wildlife.Application.Client;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Validators;
using FaunaFinder.Client.Localization;
using FaunaFinder.Client.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Tempest;

namespace FaunaFinder.Client.Components.Sightings;

// The [Command] lives in this code-behind (with the base stated explicitly)
// because Tempest's razor frontend matches the @inherits text by simple name
// and can't see that EcoDataComponent is a StatefulComponent; the C# symbol
// frontend can.
public partial class SightingDetailDialog : EcoDataComponent
{
    private const int NoteMaxLength = 2000;
    private const int DetailZoom = 13;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public required SightingDto Sighting { get; set; }

    [Parameter]
    public LocaleContext Locale { get; set; } = LocaleContext.English;

    [Inject]
    private ISightingHttpClient SightingClient { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private readonly MapController<SightingMarker> _mapController = new();
    private readonly List<SightingNoteDto> _notes = [];
    private string _noteText = string.Empty;
    private string? _noteError;

    private string CommonName => Locale.Resolve(Sighting.SpeciesCommonName, fallback: Sighting.SpeciesScientificName);

    private CultureInfo Culture => CultureInfo.GetCultureInfo(Locale.Code);

    protected override void OnInitialized()
    {
        base.OnInitialized();

        var point = new MapCoordinate(Sighting.Latitude, Sighting.Longitude);
        _mapController.SetView(point, DetailZoom);
        _mapController.SetMarkers([new SightingMarker(point)]);
        _notes.AddRange(Sighting.Notes);
    }

    [Command]
    private async Task AddNote()
    {
        _noteError = null;

        var dto = new SightingNoteDtoForCreate(_noteText.Trim());
        var validation = new SightingNoteDtoForCreateValidator().Validate(dto);
        if (!validation.IsValid)
        {
            _noteError = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage));
            return;
        }

        var result = await SightingClient.AddNoteAsync(Sighting.Id, dto);
        if (result.TryPickT1(out var validationFailed, out var remainder))
        {
            _noteError = string.Join(" ", validationFailed.AllMessages);
            return;
        }

        if (remainder.TryPickT1(out var requestFailed, out var note))
        {
            var message = requestFailed.StatusCode switch
            {
                0 => L["Sighting_Error_Unreachable"],
                401 => L["Sighting_Error_SignedOut"],
                403 => L["Sighting_Error_Forbidden"],
                404 => L["Sighting_Error_NotFound"],
                429 => L["Sighting_Error_TooMany"],
                _ => L["Sighting_Error_Generic"],
            };
            Snackbar.Add(message, Severity.Error);
            return;
        }

        _notes.Add(note);
        _noteText = string.Empty;
    }

    private void Close() => MudDialog.Close();
}
