namespace EcoData.Spa.Navigation.Events;

/// <summary>
/// Published whenever navigation state changes — a location change, or a page
/// declaring its deep-link parent. Carries the new state so a handler never has
/// to read the manager back.
///
/// <para>Top-level on purpose: the publisher is a shared service and the
/// subscribers live in app assemblies, so no single component can own this as a
/// nested record. Handle it with <c>[Event]</c> — Tempest 1.0.0-beta.8 relaxed
/// TEM001 so the record no longer has to be nested in the handling
/// component:</para>
///
/// <code>
/// [Event]
/// private void OnNavigationChanged(NavigationChanged e) => UpdateCurrentTab(e.State.Path);
/// </code>
/// </summary>
public sealed record NavigationChanged(NavigationState State);
