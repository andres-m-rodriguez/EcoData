# FaunaFinder UI Modernization Plan

Scope: port the old FaunaFinder (`C:\Users\Overlord\Downloads\FaunaFinder`) feature depth for **species** and **municipalities** into `src/Apps/FaunaFinder/`, using EcoData's design language and patterns. Out of scope: sightings, auth, admin, stats, about, export, polygon draw, heatmap, radius search, i18n/dark-mode toggles.

Source conventions to follow:
- `docs/creating-endpoints.md` — one resource per Endpoints file, typed results, `IAsyncEnumerable` for lists, cursor pagination.
- `docs/creating-components.md` — no custom CSS outside shared/landing, MudBlazor utility classes, `IFetch<T>` for data, always handle loading/empty/error states.
- `docs/REST resource-based architecture.md` — module owns its hierarchy; no cross-module resources.

## Feature gaps to close (old → current)

| Area | Old FaunaFinder | Current | Action |
|------|----------------|---------|--------|
| Species list | Infinite scroll, category chips, fauna/flora toggle, search | Flat list + search | Virtualized cursor list + filter dialog + chip row |
| Species detail | Hero image, G/S-rank badges, municipality grid, FWS/NRCS links w/ inline filter | Header only | Full rebuild |
| Municipalities | `/pueblos` list + `/pueblos/{id}` detail + species counts | Not present | New routes |
| Map home | Click-to-panel + filter dialog in sidebar | Click-to-panel, no filter | Add filter button inside panel |

## Information architecture

```
/                       Map home — click municipality → species panel
/species                List — search, category chip row, fauna/flora toggle, GRank filter dialog
/species/{id}           Detail — hero, metadata, municipality grid, conservation links
/municipalities         List — search, species-count per card
/municipalities/{id}    Detail — stat hero, species list scoped to municipality
/categories             (existing)
/categories/{id}        (existing)
```

Mobile bottom nav: **Map · Species · Municipalities · Categories**.

## Data / contracts

Wildlife `SpeciesParameters` (`Search`, `CategoryId`, `MunicipalityId`, `IsFauna`, cursor paging) covers the list. No new wildlife params needed for scope.

New/changed:

- `MunicipalityParameters : CursorParameters(PageSize, Cursor)` — current parameter record doesn't inherit from `CursorParameters`; must be updated so it works with `EcoDataVirtualizedList`.
- `GET /locations/municipalities/by-ids?ids=g1,g2,...` — new endpoint + HTTP client method to resolve `MunicipalityDtoForList[]` from an id array in one round trip (used in species detail).
- **Deferred**: GRank filter on `SpeciesParameters` (not required for phase 2 scope; add if wanted).

## Component inventory

**App-specific** (FaunaFinder.Client/Components/{Species,Municipalities,Shared}) — no custom CSS, MudBlazor utility classes only:

| Component | Role |
|-----------|------|
| `Species/SpeciesCard` | Thumbnail + common + scientific + GRank + chevron (list, muni detail, map panel) |
| `Species/SpeciesList` | EcoDataVirtualizedList with NuiSearchBar + filter button; accepts CategoryId/MunicipalityId/IsFauna parameters so reused across contexts |
| `Species/SpeciesFilterDialog` | Category multi-select, fauna/flora toggle |
| `Species/SpeciesHero` | Detail banner: image (falls back to icon), names, category chips |
| `Species/ConservationLinksPanel` | FWS/NRCS links w/ inline search + practice/action selects + removable chips |
| `Species/CategoryChipRow` | Horizontally scrolling selectable chips for quick-filter |
| `Municipalities/MunicipalityCard` | Icon + name + lazy "N species" count |
| `Municipalities/MunicipalityList` | EcoDataVirtualizedList + NuiSearchBar |
| `Shared/StatsHero` | Icon + big number + label |

**NativeUi (Nui\*) additions**: none required — everything is app-specific enough not to promote into NativeUi yet.

## Data-fetching conventions

Per `feedback_loading_state.md`: every component uses `IFetch<T>` (single resource) or `EcoDataVirtualizedList` (cursor-paginated lists). No `_isLoading` bool, no try/finally wrappers. Detail pages spawn multiple IFetches in parallel in `OnInitialized`.

## Visual style

- Primary palette: FaunaFinder greens (`#2d6a4f` / `#40916c`), kept from existing `FaunaFinderTheme`.
- Typography: Inter (body), Newsreader (headings) — already configured.
- Card pattern: `MudPaper Elevation="0" Class="pa-4 rounded-lg border-solid border mud-border-lines-default"` (matches EcoPortal info cards).
- Conservation badges via existing `NuiStatusBadge` (G1/G2 = Error, G3 = Warning, G4/G5 = Success).

## Phases

**Phase 1 — Contracts + scaffolding**
- Update `MunicipalityParameters` to inherit `CursorParameters`.
- Create shell `SpeciesCard`, `MunicipalityCard`, `StatsHero` (used across multiple phases).

**Phase 2 — `/species` list**
- Rewrite Species list view with `EcoDataVirtualizedList<SpeciesDtoForList, SpeciesParameters>`.
- `CategoryChipRow` (fetches `/wildlife/species-categories` once) driving CategoryId.
- NuiSearchBar + filter button → `SpeciesFilterDialog` for fauna/flora.
- Signal-driven refresh (`BlazingSingularity.Signals`) matching `SensorList` pattern.

**Phase 3 — `/species/{id}` detail**
- Add `GET /locations/municipalities/by-ids` endpoint + client method.
- `IFetch<SpeciesDtoForDetail>`, `IFetch<ConservationLinksDtoForSpecies>`, `IFetch<IReadOnlyList<MunicipalityDtoForList>>` kicked off in parallel.
- Compose: `SpeciesHero` + metadata card + municipality grid (`MunicipalityCard`) + `ConservationLinksPanel`.

**Phase 4 — Municipality routes**
- New `/municipalities` and `/municipalities/{id}` routes.
- `MunicipalityList` → `MunicipalityCard` (lazy species-count via `IFetch<int>` against `/wildlife/species/count?municipalityId=`).
- Detail: `StatsHero` + reused `SpeciesList` bound to `MunicipalityId`.

**Phase 5 — Map home refinements**
- Replace ad-hoc `NuiListItem` list inside the sidebar/bottom-sheet with `SpeciesCard`.
- Optional filter button in sheet header → existing `SpeciesFilterDialog`.

**Phase 6 — Navigation polish**
- Bottom nav: add **Municipalities** tab.
- Desktop AppBar links: Map / Species / Municipalities / Categories.
- Verify every non-home page wraps in `NuiPageLayout`.

## Deferred open decisions (chosen defaults)

1. `municipalities/by-ids` endpoint — **adopted** in Phase 3 (vs N round-trips).
2. Species count on `MunicipalityCard` — **lazy per-card** `IFetch<int>` (acceptable at PageSize 20).
3. Species images in cards — **lazy on render** via `<img src=/wildlife/species/{id}/image loading=lazy>`.
4. GRank filter — **deferred** (no contract change needed in this scope).

## Acceptance per phase

Each phase ends with: clean `dotnet build`, route reachable in browser, skeleton/loaded/empty/filtered-empty states visibly distinct.
