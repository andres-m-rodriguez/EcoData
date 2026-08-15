using EcoData.Spa.Contracts.Navbar;

namespace EcoData.Spa.Contracts.Events;

/// <summary>
/// Published whenever the navbar title or actions change. Carries the new state
/// so a handler never has to read the manager back.
///
/// <para>Top-level for the same reason as <see cref="NavigationChanged"/>: the
/// publisher is a shared service, the subscribers are app chrome. Handle it with
/// <c>[Event]</c>.</para>
/// </summary>
public sealed record NavbarChanged(NavbarState State);
