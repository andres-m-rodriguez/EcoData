using EcoData.Common.i18n;
using Microsoft.AspNetCore.Components;
using Tempest;

namespace EcoData.Spa.Blazor;

/// <summary>
/// The base every EcoData component renders from: a Tempest
/// <see cref="StatefulComponent"/> that also carries the cross-cutting concerns
/// each app would otherwise wire by hand — today localization.
///
/// <para><see cref="ILocalizer"/> is injected as <c>L</c> and its
/// <see cref="ILocalizer.LanguageChanged"/> is subscribed once here, so a
/// subclass writes <c>@L["SomeKey"]</c> and re-renders on a locale flip without
/// a <c>+=</c>, a matching <c>-=</c>, or a hand-written
/// <c>InvokeAsync(StateHasChanged)</c> of its own.</para>
///
/// <para>Override <see cref="OnLanguageChanged"/> for work beyond re-rendering —
/// re-titling the navbar, refetching locale-dependent data. The re-render happens
/// either way.</para>
///
/// <para>Layouts use <see cref="EcoDataLayout"/> instead; C# has no multiple
/// inheritance, so the two are mirrors. Change both when changing either.</para>
///
/// <para><b>Tempest attributes must live in a code-behind.</b> Tempest's razor
/// frontend matches <c>@inherits</c> by simple name and cannot see that this type
/// derives from <see cref="StatefulComponent"/>, so a <c>[Command]</c>,
/// <c>[Reactive]</c> or <c>[Event]</c> member written inside an <c>@code</c> block
/// under <c>@inherits EcoDataComponent</c> raises TEM002. Declared in a
/// <c>.razor.cs</c> partial it resolves fine — the C# symbol frontend walks the
/// real base chain.</para>
/// </summary>
public abstract class EcoDataComponent : StatefulComponent
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

    // Mutate is Tempest's marshal-run-rerender primitive, so the hook lands on
    // the renderer thread and costs exactly one re-render.
    private void HandleLanguageChanged() => _ = Mutate(OnLanguageChanged);

    public override void Dispose()
    {
        L.LanguageChanged -= HandleLanguageChanged;
        base.Dispose();
    }
}
