using EcoData.Common.i18n;
using Microsoft.AspNetCore.Components;
using Tempest;

namespace FaunaFinder.Client.Localization;

/// <summary>
/// Base for any component that renders strings from <see cref="ILocalizer"/>.
/// Subscribes to <see cref="ILocalizer.LanguageChanged"/> in <see cref="OnInitialized"/>
/// and re-renders when the locale flips, so call-sites can write
/// <c>@L["SomeKey"]</c> without wiring the subscription themselves.
/// Inherits <see cref="StatefulComponent"/> so subclasses get Tempest support.
///
/// <para>Subclasses that need their own init or disposal logic should call
/// <c>base.OnInitialized()</c> / <c>base.Dispose()</c>.</para>
/// </summary>
public abstract class LocalizedComponentBase : StatefulComponent
{
    [Inject]
    protected ILocalizer L { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        L.LanguageChanged += HandleLanguageChanged;
    }

    private void HandleLanguageChanged() => InvokeAsync(StateHasChanged);

    public override void Dispose()
    {
        L.LanguageChanged -= HandleLanguageChanged;
        base.Dispose();
    }
}
