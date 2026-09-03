using EcoData.Common.i18n;
using Microsoft.AspNetCore.Components;
using Tempest;

namespace EcoData.Spa.Blazor;

/// <summary>
/// The layout flavour of <see cref="EcoDataComponent"/>: same injected
/// <see cref="ILocalizer"/> and the same <see cref="OnLanguageChanged"/> hook, but
/// inheriting <see cref="StatefulLayoutComponent"/> so Blazor accepts it as a
/// layout.
///
/// <para>Kept as a mirror of <see cref="EcoDataComponent"/> because C# has no
/// multiple inheritance — change both when changing either. Tempest splits its own
/// bases the same way and for the same reason.</para>
///
/// <para>The code-behind rule from <see cref="EcoDataComponent"/> applies here
/// too: Tempest attributes belong in a <c>.razor.cs</c> partial.</para>
/// </summary>
public abstract class EcoDataLayout : StatefulLayoutComponent
{
    [Inject]
    protected ILocalizer L { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        L.LanguageChanged += HandleLanguageChanged;
    }

    /// <summary>
    /// Called when the locale flips, before the re-render. Empty by default —
    /// override only when a language change means more than redrawing.
    /// </summary>
    protected virtual void OnLanguageChanged()
    {
    }

    private void HandleLanguageChanged() => _ = Mutate(() => OnLanguageChanged());

    public override void Dispose()
    {
        L.LanguageChanged -= HandleLanguageChanged;
        base.Dispose();
    }
}
