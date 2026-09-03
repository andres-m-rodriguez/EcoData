using System.Globalization;
using EcoData.Spa.Blazor;
using EcoData.Spa.Map;
using EcoData.Wildlife.Application.Client;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Validators;
using FaunaFinder.Client.Localization;
using FaunaFinder.Client.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
    private const int MaxImages = 5;
    private const long MaxImageBytes = 10 * 1024 * 1024;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public required SightingDto Sighting { get; set; }

    [Parameter]
    public LocaleContext Locale { get; set; } = LocaleContext.English;

    // The reporter adds and removes photos; a reviewer only looks.
    [Parameter]
    public bool CanEdit { get; set; }

    // Resolved by the caller; Wildlife carries only the id.
    [Parameter]
    public string? MunicipalityName { get; set; }

    // Wired by the review page. A delegate on OnApprove puts the dialog in
    // reviewer mode: the decision buttons join the footer and the approval
    // note shows under the map.
    [Parameter]
    public EventCallback<SightingDto> OnApprove { get; set; }

    [Parameter]
    public EventCallback<SightingDto> OnDeny { get; set; }

    [Parameter]
    public EventCallback<SightingDto> OnUnapprove { get; set; }

    [Inject]
    private ISightingHttpClient SightingClient { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IDialogService Dialogs { get; set; } = default!;

    private readonly MapController<SightingMarker> _mapController = new();
    private readonly List<SightingNoteDto> _notes = [];
    private readonly List<SightingImageDto> _images = [];
    private readonly List<IBrowserFile> _pendingFiles = [];
    private SightingImageDto? _viewing;
    private string _noteText = string.Empty;
    private string? _noteError;

    private string CommonName => Locale.Resolve(Sighting.SpeciesCommonName, fallback: Sighting.SpeciesScientificName);

    private bool CanReview => OnApprove.HasDelegate;

    private CultureInfo Culture => CultureInfo.GetCultureInfo(Locale.Code);

    private string ImageUrl(SightingImageDto image) => $"/wildlife/sightings/{Sighting.Id}/images/{image.Id}";

    protected override void OnInitialized()
    {
        base.OnInitialized();

        var point = new MapCoordinate(Sighting.Latitude, Sighting.Longitude);
        _mapController.SetView(point, DetailZoom);
        _mapController.SetMarkers([new SightingMarker(point)]);
        _notes.AddRange(Sighting.Notes);
        _images.AddRange(Sighting.Images);
    }

    [Command]
    private async Task AddNote()
    {
        _noteError = null;

        var dto = new SightingNoteDtoForCreate(_noteText.Trim());
        var validation = new SightingNoteDtoForCreateValidator().Validate(dto);
        if (!validation.IsValid)
        {
            var messages = validation.Errors.Select(e => e.ErrorMessage);
            _noteError = string.Join(" ", messages);
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

    private void OnFilesPicked(IReadOnlyList<IBrowserFile>? picked)
    {
        if (picked is null) return;

        _pendingFiles.Clear();
        foreach (var file in picked)
        {
            if (_images.Count + _pendingFiles.Count >= MaxImages)
            {
                Snackbar.Add(L["Sighting_Image_TooMany", MaxImages], Severity.Warning);
                break;
            }

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                Snackbar.Add(L["Sighting_Image_NotImage", file.Name], Severity.Warning);
                continue;
            }

            if (file.Size > MaxImageBytes)
            {
                Snackbar.Add(L["Sighting_Image_TooLarge", file.Name], Severity.Warning);
                continue;
            }

            _pendingFiles.Add(file);
        }

        if (_pendingFiles.Count > 0)
            _ = UploadPendingState.TryExecute();
    }

    [Command]
    private async Task UploadPending()
    {
        foreach (var file in _pendingFiles)
        {
            await using var content = file.OpenReadStream(MaxImageBytes);
            var result = await SightingClient.UploadImageAsync(Sighting.Id, content, file.Name, file.ContentType);
            if (!result.TryPickT0(out var image, out _))
            {
                Snackbar.Add(L["Sighting_Image_UploadFailed", file.Name], Severity.Warning);
                continue;
            }
            _images.Add(image);
        }
        _pendingFiles.Clear();
    }

    private async Task DeleteImage(SightingImageDto image)
    {
        var confirmed = await Dialogs.ShowMessageBoxAsync(
            L["Sighting_Image_Delete"],
            L["Sighting_Image_Delete_Confirm"],
            yesText: L["Sighting_Image_Delete"],
            cancelText: L["Common_Cancel"]);
        if (confirmed != true) return;

        var result = await SightingClient.DeleteImageAsync(Sighting.Id, image.Id);
        if (!result.IsT0)
        {
            Snackbar.Add(L["Sighting_Image_DeleteFailed"], Severity.Error);
            return;
        }

        _images.Remove(image);
        if (_viewing == image) _viewing = null;
    }

    private void Close() => MudDialog.Close();
}
