using System.Net;
using EcoData.Spa.Blazor;
using EcoData.Wildlife.Application.Client;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Validators;
using FaunaFinder.Client.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Tempest;

namespace FaunaFinder.Client.Components.Sightings;

// The [Command] lives in this code-behind (with the base stated explicitly)
// because Tempest's razor frontend matches the @inherits text by simple name
// and can't see that EcoDataComponent is a StatefulComponent; the C# symbol
// frontend can.
public partial class SightingReviewDialog : EcoDataComponent
{
    private const int ReasonMaxLength = 1000;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public required SightingDto Sighting { get; set; }

    [Parameter, EditorRequired]
    public Guid OrganizationId { get; set; }

    // True approves, false denies. The reason is optional on approve and
    // required on deny; each side validates with its own contract validator.
    [Parameter]
    public bool Approve { get; set; }

    [Parameter]
    public LocaleContext Locale { get; set; } = LocaleContext.English;

    [Inject]
    private ISightingHttpClient SightingClient { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private string _reason = string.Empty;
    private string? _reasonError;

    [Command]
    private async Task Submit()
    {
        _reasonError = null;
        var reason = string.IsNullOrWhiteSpace(_reason) ? null : _reason.Trim();

        var validation = Approve
            ? new SightingApprovalDtoValidator().Validate(new SightingApprovalDto(reason))
            : new SightingDenialDtoValidator().Validate(new SightingDenialDto(reason ?? string.Empty));
        if (!validation.IsValid)
        {
            var messages = validation.Errors.Select(e => e.ErrorMessage);
            _reasonError = string.Join(" ", messages);
            return;
        }

        var result = Approve
            ? await SightingClient.ApproveAsync(OrganizationId, Sighting.Id, new SightingApprovalDto(reason))
            : await SightingClient.DenyAsync(OrganizationId, Sighting.Id, new SightingDenialDto(reason ?? string.Empty));
        if (result.TryPickT1(out var validationFailed, out var remainder))
        {
            _reasonError = string.Join(" ", validationFailed.AllMessages);
            return;
        }

        if (remainder.TryPickT1(out var requestFailed, out _))
        {
            var message = requestFailed.StatusCode switch
            {
                HttpStatusCode.Unauthorized => L["Sighting_Error_SignedOut"],
                HttpStatusCode.Forbidden => L["Sighting_Review_Error_Forbidden"],
                HttpStatusCode.NotFound => L["Sighting_Review_Error_NotFound"],
                HttpStatusCode.TooManyRequests => L["Sighting_Error_TooMany"],
                _ when requestFailed.IsTransportFailure => L["Sighting_Error_Unreachable"],
                _ => L["Sighting_Error_Generic"],
            };
            Snackbar.Add(message, Severity.Error);
            return;
        }
        var decided = DialogResult.Ok(true);
        MudDialog.Close(decided);
    }

    private void Cancel() => MudDialog.Cancel();
}
