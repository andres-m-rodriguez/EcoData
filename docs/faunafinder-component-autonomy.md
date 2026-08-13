# FaunaFinder — component autonomy scan

Which components should get the treatment `FfBottomNav` just got, and which
should be left alone.

## What the bottom-nav change actually was

Three separable moves, and it matters that they're separable — most
candidates below want one or two of them, not all three:

1. **Extraction** — the markup and its state left `MainLayout`, which no
   longer knows what a tab is.
2. **Self-sufficiency** — the component sources its own state (the active
   tab, from `INativeNavigationManager`) instead of being told.
3. **Events instead of plumbing** — it declares nested `Hidden` / `Shown`
   records with `[Event]` handlers, so any screen can control it by
   publishing on the bus. The layout never learns that the map has a sheet.

Move 3 is the one worth being careful about. `[Event]` binds only to a record
declared on the *handling* component, so events suit **cross-cutting control**
— one screen influencing chrome it doesn't own. Ordinary parent-to-child
configuration is what parameters are for, and converting that to events makes
data flow harder to follow, not easier.

## How the candidates were scored

`par` = `[Parameter]` count, `cb` = `EventCallback` count, `inj` = injected
services, `cmd` = Tempest `[Command]`s. High `par`/`cb` with zero `inj`/`cmd`
is the signature of a component that is told everything and discovers nothing.

For context on why the map cluster dominates: **`Map.razor` is 815 lines with
74 private fields**, most of them held on behalf of its children.

## The table

| Component | par / cb / inj / cmd | What the parent owns on its behalf | Recommended change | Event to expose | Priority |
|---|---|---|---|---|---|
| `MapConservationFilterDialog` | 9 / 4 / 0 / 0 | Visibility, both available-code lists, both selection sets, toggle + clear handlers | Fetch its own codes for the municipality; own the selection | `FiltersChanged(nrcs, fws)` | **1 — highest** |
| `MapSpeciesPanel` | 11 / 4 / 1 / 0 | `TotalCount`, `FilteredCount`, `ActiveFilterCount`, both code arrays | Subscribe to `FiltersChanged`; fetch its own counts | — (consumer) | **2** |
| `SpeciesToolbar` | 8 / 4 / 0 / 0 | Search text, sort, filter result, stats — then relayed to `SpeciesGrid` | Own search/sort/filter; publish once | `QueryChanged(params)` | **3** |
| `MapNearbyPanel` | 7 / 3 / 0 / 0 | The result list and its loading flag, from the page's `SearchAreaState` | Take origin/polygon as params, fetch its own results | — (consumer) | 4 |
| `MunicipalityList` | 5 / 2 / 0 / 0 | The 200-item list, the counts map, the selected id | See caveat below — needs a shared source, not two fetches | `MunicipalitySelected(id)` | 5 |
| `MunicipalityMap` | 6 / 3 / 0 / 0 | Same list, same counts, same selection | Same caveat | consumes `MunicipalitySelected` | 5 |
| `MapLocationNavigator` | 6 / 4 / 0 / 0 | Current index; every button is a callback | Own the index; keep focus as a callback | — | 6 |
| `SpeciesFilterDialog` | 3 / 0 / 0 / 0 | Facets and taxon counts | Fetch its own facets | — | 7 |

## Leave these alone

| Component | Why |
|---|---|
| `SpeciesGrid` | 8 params but 3 injections and self-fetches — its parameters *are* query configuration. This is already the target shape. |
| `SpeciesFeaturedRow`, `MunicipalityCard`, `ConservationLinksPanel` | Already self-sufficient: own `[Command]`s, 1–2 params. |
| `StatsHero`, `SpeciesEditorialHero`, `PracticeEditorialHero`, `ActionEditorialHero`, `MunicipalityStatsRow`, `SpeciesStatsRow` | Presentational. Being handed a DTO is correct; giving them fetches would duplicate requests across a page. |
| `SpeciesCard`, `NuiListItem`-style leaves | Pure render. Nothing to own. |

## The one that isn't a simple win

`MunicipalityList` and `MunicipalityMap` both receive the same 200-municipio
list plus the same counts dictionary, and both raise `OnSelected` — the page
is a pure relay between them, which is exactly the smell. But making each
self-sufficient would issue **two identical 200-row fetches plus two count
fetches** per page load.

That pair wants a shared scoped service holding the list, with a
`MunicipalitySelected` event for the coordination — not the `FfBottomNav`
treatment applied twice. Worth doing, but it is a different change.

## Suggested order

1 and 2 are one piece of work: the filter selection is currently threaded from
`Map.razor` into *both* the dialog and the panel, so moving ownership into the
dialog and letting the panel subscribe removes both sets of plumbing at once.
Expect `Map.razor` to shed the `_selectedNrcsPractices` / `_selectedFwsActions`
sets, `AvailableNrcsPractices` / `AvailableFwsActions`, `ToggleNrcsPractice`,
`ToggleFwsAction`, `ClearConservationFilters`, `ClearConservationFiltersAndReload`,
and the counts command — roughly 60 lines and 6 fields off the largest page in
the app.

3 is independent and self-contained: `SpeciesToolbar` publishing a query that
`SpeciesGrid` consumes removes `Species.razor` from the middle of its own
filtering.

4 onwards are cleanups, not structural wins.
