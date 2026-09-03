using EcoData.Spa.Blazor;
using EcoData.Wildlife.Contracts.Dtos;
using FaunaFinder.Client.Components.Sightings;
using FaunaFinder.Client.Layout;
using FaunaFinder.Client.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Tempest;

namespace FaunaFinder.Client.Pages;

// The [Event] lives in this code-behind (with the base stated explicitly)
// because Tempest's razor frontend matches the @inherits text by simple name
// and can't see that EcoDataComponent is a StatefulComponent; the C# symbol
// frontend can.
public partial class MySightings : EcoDataComponent
{
    private const int PageSize = 20;

    // Keys held as consts so they can appear in Razor attribute forms
    // (`Title="@L[SomeKey]"`) without tripping the nested-quote rule.
    private const string EmptyTitleKey = "Sighting_Mine_Empty_Title";
    private const string EmptyDescriptionKey = "Sighting_Mine_Empty_Description";
    private const string UnreachableKey = "Sighting_Error_Unreachable";
    private const string LoadFailedKey = "Sighting_Error_Generic";

    [CascadingParameter]
    public LocaleContext Locale { get; set; } = LocaleContext.English;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Navbar.SetTitle(L["Sighting_Mine_Title"]);
        RedirectIfSignedOut();
    }

    protected override void OnLanguageChanged() => Navbar.SetTitle(L["Sighting_Mine_Title"]);

    [Event]
    private void OnAuthChanged(MainLayout.AuthChanged _) => RedirectIfSignedOut();

    private void RedirectIfSignedOut()
    {
        if (!Auth.IsInitialized || Auth.IsAuthenticated) return;

        var here = "/" + NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.NavigateTo($"/login?ReturnUrl={Uri.EscapeDataString(here)}", replace: true);
    }

    private async Task OpenDetail(SightingDto sighting)
    {
        var breakpoint = await Viewport.GetCurrentBreakpointAsync();

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            FullScreen = breakpoint is Breakpoint.Xs or Breakpoint.Sm,
        };

        // The dialog provider sits outside the layout's LocaleContext cascade,
        // so the locale travels as a parameter.
        var parameters = new DialogParameters<SightingDetailDialog>
        {
            { x => x.Sighting, sighting },
            { x => x.Locale, Locale },
            { x => x.CanEdit, true },
        };

        await Dialogs.ShowAsync<SightingDetailDialog>(null, parameters, options);
    }
}
