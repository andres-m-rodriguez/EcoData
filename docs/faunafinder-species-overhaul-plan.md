# FaunaFinder — Species Page Overhaul Plan

Source of truth for the redesign: `FaunaMockup/` (React + plain CSS field-guide design).
Target: `src/Apps/FaunaFinder/FaunaFinder.Client/Pages/Species.razor` and supporting components / contracts / data.

The overhaul is split into two sequenced plans:

- **Plan 1 — Backend & Contracts** (schema, DTOs, endpoints, seeding) — must land first.
- **Plan 2 — UI** (Blazor components, theme, CSS) — assumes Plan 1's columns/endpoints exist.

Out of scope for both plans:
- The "Sort" affordance is decorative in the mockup (no real sort menu drives anything actionable). Implement only if also adding sort filter parameters; otherwise the toolbar control becomes a static label.
- Pagination row is decorative — list stays cursor-paged with infinite scroll (matches `EcoDataVirtualizedList`). Mockup itself says "scrolling loads more records".
- The "Tweaks" panel from the mockup (designer-only edit-mode) is not shipped.
- Footer (`.ff-footer` block) — no parity for marketing footer in this app.

---

## Mockup → Current state gap matrix

| Mockup feature | Current backing | Action |
|---|---|---|
| Editorial hero with eyebrow + meta line | `NuiPageLayout` plain title | UI-only: new `SpeciesEditorialHero` component |
| 4-stat row (total / endemic / threatened / municipios) | `/wildlife/species/count` only | **Backend**: stats endpoint + `IsEndemic` column |
| "▲ 12 this quarter" delta | none | **Backend**: `CreatedAtUtc` column on Species + delta in stats |
| Featured row (1 large + 2 medium) | none | **Backend**: `IsFeatured` flag + `/wildlife/species/featured` endpoint |
| Search by common, scientific OR municipio name | scientific only (`Repository.ApplyFilters`) | **Backend**: extend `Search` matching to common names + municipality names |
| Filter chip "Taxon: Birds, Reptiles, Plants" with 8 fixed taxa + counts | generic `SpeciesCategory` table (no fixed taxonomy) | **Backend**: seed 8 standardized `SpeciesCategory` codes (`bird`, `plant`, `reptile`, `amphib`, `fish`, `mammal`, `invert`, `fungi`) + facet counts endpoint |
| Filter chip "Status: EN, CR" w/ IUCN codes (LC/NT/VU/EN/CR/DD) | only `GRank`/`SRank` (NatureServe) | **Backend**: `IucnStatus` enum column + facet counts endpoint |
| "Endemic only" qualifier | none | **Backend**: `IsEndemic` column + filter parameter |
| "Observed in last 12 months" qualifier | none | **Backend**: `LastObservedAtUtc` column + filter parameter |
| "Has high-resolution imagery" qualifier | only `HasProfileImage` bool | **Backend**: stretch — promote to threshold (we don't track resolution). Recommendation: rename UI label to "Has photo" and reuse `HasProfileImage` filter; do not add a column. |
| "Minimum municipios present" slider | none (we have count derivable from `MunicipalitySpecies`) | **Backend**: `MinMunicipalityCount` filter parameter |
| Card footer "5 municipios" | `SpeciesDtoForList` has no count | **Backend**: add `MunicipalityCount` to `SpeciesDtoForList` |
| Card footer "lastSeen 3d ago" / "Verified" | none | **Backend**: include `LastObservedAtUtc` in `SpeciesDtoForList` |
| Endemic badge on cards | none | depends on `IsEndemic` (Plan 1) |
| Card grid layout (4 cols, 4:3 image, taxa icon overlay, status pill, hover lift, grayscale→color) | `SpeciesCard` is a flat MudPaper row | UI-only: rewrite `SpeciesCard` (kept name, new template) |
| Filter modal layout (taxa grid, status list, qualifiers, range slider) | `SpeciesFilterDialog` is a 2-select dropdown | UI-only: rewrite (uses new filter params from Plan 1) |
| Theme: deep pine `#1f4d3a` primary, warm paper bg `#f4f4ef` | `FaunaFinderTheme` uses `#2d6a4f` and `#f8f9fa` | UI-only: edit `FaunaFinderTheme.cs` + add design-token CSS |
| Toolbar-level search + filter button + active chip row | `SpeciesList` already has `NuiSearchBar` + filter dialog + active chip stack | UI-only: restyle to match editorial layout |
| Lang toggle EN/ES in topbar, "Contribute" CTA, account icon | not in scope | skip — keep current `MainLayout` |

---

# Plan 1 — Backend & Contracts

Scope: schema migration, DTO updates, repository changes, new endpoints, HTTP client surface, seeder updates. UI is **not** touched here; the existing UI keeps compiling against the new contracts (additive, plus filter param extensions).

## 1.1 Schema additions to `Species`

File: `src/Features/Wildlife/EcoData.Wildlife.Database/Models/Species.cs`

Add columns (keep `required` discipline):

```csharp
public required bool IsEndemic { get; set; }                  // PR-endemic flag
public required IucnStatus? IucnStatus { get; set; }          // LC/NT/VU/EN/CR/DD/EX/null
public required bool IsFeatured { get; set; }                 // editorial pick
public string? Habitat { get; set; }                          // short label, optional
public DateTimeOffset? LastObservedAtUtc { get; set; }        // denormalized from sightings
public DateTimeOffset CreatedAtUtc { get; set; }              // for "this quarter" delta
```

New enum `EcoData.Wildlife.Contracts.IucnStatus { LC, NT, VU, EN, CR, DD, EX }` in the contracts assembly so DTOs and parameters can reference it without leaking the database.

`EntityConfiguration` updates:
- `IucnStatus` stored as `string` (8-char max) via `HasConversion<string>()`.
- Add `HasIndex(s => s.IsFeatured).HasFilter("is_featured = true")` for the featured row query.
- Add `HasIndex(s => s.IucnStatus)` for facet counts.
- Add `HasIndex(s => s.IsEndemic).HasFilter("is_endemic = true")`.
- `CreatedAtUtc` defaults to `now()` at column level (`HasDefaultValueSql("now()")`).

Single migration: `AddSpeciesEditorialFields`.

## 1.2 DTO additions

File: `src/Features/Wildlife/EcoData.Wildlife.Contracts/Dtos/SpeciesDtos.cs`

```csharp
public sealed record SpeciesDtoForList(
    Guid Id,
    IReadOnlyList<LocaleValue> CommonName,
    string ScientificName,
    bool IsFauna,
    string GRank,
    string SRank,
    bool HasProfileImage,
    // NEW:
    bool IsEndemic,
    IucnStatus? IucnStatus,
    string? TaxonCode,                 // resolved primary category code (bird/plant/...)
    int MunicipalityCount,
    DateTimeOffset? LastObservedAtUtc,
    bool IsFeatured
);

public sealed record SpeciesDtoForDetail(
    // ... existing fields ...
    bool IsEndemic,
    IucnStatus? IucnStatus,
    string? Habitat,
    DateTimeOffset? LastObservedAtUtc
);

// Extend create/update DTOs to accept the new fields.
```

`SpeciesDtoForList` projection in `SpeciesRepository.GetSpeciesAsync` must:
- Project `s.MunicipalitySpecies.Count` (cheap; covered by the existing FK index).
- Project the first `CategoryLinks.Select(cl => cl.Category.Code).FirstOrDefault()` as `TaxonCode`.

## 1.3 Filter parameters

File: `src/Features/Wildlife/EcoData.Wildlife.Contracts/Parameters/SpeciesParameters.cs`

```csharp
public sealed record SpeciesParameters(
    int PageSize = 20,
    Guid? Cursor = null,
    string? Search = null,
    Guid? CategoryId = null,
    Guid? MunicipalityId = null,
    bool? IsFauna = null,
    // NEW:
    bool? IsEndemic = null,
    bool? HasProfileImage = null,
    IReadOnlyList<IucnStatus>? IucnStatuses = null,   // multi-select
    IReadOnlyList<string>? TaxonCodes = null,         // multi-select on category code
    int? MinMunicipalityCount = null,
    DateTimeOffset? ObservedSinceUtc = null,
    SpeciesSort Sort = SpeciesSort.ScientificNameAsc
) : CursorParameters(PageSize, Cursor);

public enum SpeciesSort { ScientificNameAsc, ScientificNameDesc, RecentlyObserved, MostMunicipalities }
```

`Repository.ApplyFilters` extension:
- `Search`: change to `EF.Functions.ILike` (or `ToLower().Contains`) across `ScientificName`, the JSON `CommonName.Value`, and any joined `MunicipalitySpecies.Municipality.Name`. Common-name/municipality search requires `Include`/sub-`Any` queries — verify the existing JSON-owned `CommonName` collection is queryable in PostgreSQL (EF Core supports `OwnsMany.ToJson()` membership).
- `IucnStatuses`: `s => statuses.Contains(s.IucnStatus.Value)`.
- `TaxonCodes`: `s.CategoryLinks.Any(cl => codes.Contains(cl.Category.Code))`.
- `MinMunicipalityCount`: `s.MunicipalitySpecies.Count >= n`.
- `ObservedSinceUtc`: `s.LastObservedAtUtc >= ts`.
- `IsEndemic`, `HasProfileImage`: simple bool filters.
- `Sort`: replace the hardcoded `OrderByDescending(s => s.Id)` ordering with a switch. **Note**: cursor pagination on a non-id column requires a composite cursor or that the chosen sort key be unique. Recommendation: keep Id as the tiebreaker, encode the sort in the cursor (or restrict cursor pagination to `ScientificNameAsc` + `Id`).
- HTTP client `SpeciesHttpClient.GetSpeciesAsync` must include the new params in `QueryStringBuilder`. Multi-value params (`iucnStatuses`, `taxonCodes`) need `.Add("iucnStatuses", parameters.IucnStatuses)` repeating.

## 1.4 New endpoints

File: `src/Features/Wildlife/EcoData.Wildlife.Api/Endpoints/SpeciesEndpoints.cs`

```csharp
// 1. Editorial stats — single trip for the StatsRow.
group.MapGet("/stats", async (
    ISpeciesRepository repository,
    CancellationToken ct) => Results.Ok(await repository.GetStatsAsync(ct)))
    .WithName("GetSpeciesStats");

// 2. Facet counts — drives the filter modal numbers.
group.MapGet("/facets", async (
    [AsParameters] SpeciesParameters parameters,   // facets respect current filters
    ISpeciesRepository repository,
    CancellationToken ct) => Results.Ok(await repository.GetFacetsAsync(parameters, ct)))
    .WithName("GetSpeciesFacets");

// 3. Featured row.
group.MapGet("/featured", async (
    ISpeciesRepository repository,
    CancellationToken ct) => Results.Ok(await repository.GetFeaturedAsync(ct)))
    .WithName("GetFeaturedSpecies");
```

Repository contract additions in `ISpeciesRepository`:

```csharp
Task<SpeciesStatsDto> GetStatsAsync(CancellationToken ct = default);
Task<SpeciesFacetsDto> GetFacetsAsync(SpeciesParameters parameters, CancellationToken ct = default);
Task<IReadOnlyList<SpeciesDtoForList>> GetFeaturedAsync(CancellationToken ct = default);
```

DTOs:

```csharp
public sealed record SpeciesStatsDto(
    int TotalSpecies,
    int EndemicCount,
    int ThreatenedCount,           // VU + EN + CR
    int MunicipalitiesCovered,
    int TotalMunicipalities,        // 78 (from locations module — see 1.6)
    int AddedThisQuarter,           // CreatedAtUtc within last 90 days
    int ReclassifiedThisQuarter     // optional; can be 0 if untracked
);

public sealed record TaxonFacetDto(string Code, int Count);
public sealed record IucnFacetDto(IucnStatus Status, int Count);
public sealed record SpeciesFacetsDto(
    IReadOnlyList<TaxonFacetDto> Taxa,
    IReadOnlyList<IucnFacetDto> Statuses,
    int EndemicCount,
    int RecentlyObservedCount,
    int WithImageCount
);
```

`HttpClient` (`SpeciesHttpClient`) gains `GetStatsAsync`, `GetFacetsAsync`, `GetFeaturedAsync` mirroring the endpoints.

## 1.5 Search across joined columns

Update `ApplyFilters` to include common-name + municipality-name matches. Cleanest with PG `ILIKE`:

```csharp
var pattern = $"%{search.Replace("%","\\%").Replace("_","\\_")}%";
query = query.Where(s =>
    EF.Functions.ILike(s.ScientificName, pattern) ||
    s.CommonName.Any(c => EF.Functions.ILike(c.Value, pattern)) ||
    s.MunicipalitySpecies.Any(ms => EF.Functions.ILike(ms.Municipality.Name, pattern)));
```

Verify the JSON `OwnsMany` collection supports `.Any(...)` translation in your EF Core version; if not, fall back to `EF.Functions.JsonContains` or persist a denormalized lower-case search column.

## 1.6 Cross-module: total municipalities

`MunicipalitiesCovered = X / 78` needs the denominator. Two options:
- **A (preferred, no cross-module call):** seed/configuration constant — Puerto Rico has 78 municipios; expose as `WildlifeOptions.TotalMunicipalitiesInRegion = 78`. Cheaper, no inter-feature call.
- **B:** call `IMunicipalityRepository.GetCountAsync` from `SpeciesRepository`. Adds a Wildlife → Locations dependency that doesn't exist today; not recommended. Stick with **A**.

## 1.7 Seeding

File: `src/Host/EcoData.Seeder/DatabaseSeederWorker.cs` (`SeedWildlifeAsync`)

- Insert/upsert the 8 fixed `SpeciesCategory` rows with codes: `bird`, `plant`, `reptile`, `amphib`, `fish`, `mammal`, `invert`, `fungi` (with EN + ES `LocaleValue` names). Idempotent on `Code`.
- Backfill seeded species: assign one of the 8 codes via `SpeciesCategoryLink`, populate `IsEndemic`, `IucnStatus` (mapping table from `GRank`: `G1→CR`, `G2→EN`, `G3→VU`, `G4→NT`, `G5→LC`, `GH/GX→EX`, `GNR/GU→DD`), and seed 3 `IsFeatured = true` rows.
- Backfill `CreatedAtUtc = now()` (default-value SQL on the column makes this automatic for seed inserts).
- `LastObservedAtUtc`: leave null until an observations feature exists, or seed random recent dates only on featured rows.

## 1.8 Acceptance for Plan 1

- `dotnet build` is clean.
- `dotnet ef database update` succeeds against a fresh DB.
- The seeder runs end-to-end and produces:
  - 8 species categories with stable codes.
  - At least 3 featured species.
  - `IsEndemic`/`IucnStatus` populated where derivable.
- `GET /wildlife/species/stats` returns non-zero counts.
- `GET /wildlife/species/facets` returns taxa + status counts that sum to the total.
- `GET /wildlife/species?taxonCodes=bird&iucnStatuses=EN,CR` returns the filtered subset.
- Existing pages (`Pages/Species.razor`, `Home.razor`) still render — DTO additions are additive.

---

# Plan 2 — UI Implementation

Assumes Plan 1 is merged: stats / facets / featured endpoints exist; `SpeciesDtoForList` carries `IsEndemic`, `IucnStatus`, `TaxonCode`, `MunicipalityCount`, `LastObservedAtUtc`, `IsFeatured`; filter parameters accept multi-select taxa + statuses + qualifiers + min municipios.

## 2.1 Theme: forest-green palette

File: `src/Apps/FaunaFinder/FaunaFinder.Client/Themes/FaunaFinderTheme.cs`

| Slot | Old | New (mockup) |
|---|---|---|
| `Primary` | `#2d6a4f` | `#1f4d3a` (deep pine) |
| `PrimaryDarken` | `#1b4332` | `#163b2c` |
| `PrimaryLighten` | `#40916c` | `#3f7d5f` |
| `Secondary` | `#40916c` | `#3f7d5f` (links / accent) |
| `Tertiary` | `#74542c` | `#8a6f3e` (field-guide brass — endemic) |
| `AppbarBackground` | `#2d6a4f` | `#1f4d3a` |
| `Background` | `#f8f9fa` | `#f4f4ef` (warm paper) |
| `BackgroundGray` | `#f0f1f3` | `#eceae1` |
| `LinesDefault` | `#c1c8c2` | `#e5e3dc` |

Dark mode: shift `Primary` to `#5a9b7a` and keep AppBar dark (`#1c1c1e`) — mockup is light-only but we keep dark working.

## 2.2 Design tokens CSS

New file: `src/Apps/FaunaFinder/FaunaFinder.Client/wwwroot/css/fauna-tokens.css`

Port the variable definitions from `FaunaMockup/styles/fauna-tokens.css` (`--fauna-primary*`, `--fauna-line`, `--fauna-bg-alt`, `--status-*`, `--status-*-bg`). Keep the IUCN status-pill colors so the new card can read `style="background:var(--status-en-bg);color:var(--status-en)"` without per-component logic.

Reference it from `App.razor` (`<link>` in head) — already has `app.css` load, add this one before it.

## 2.3 New / rewritten components

App-specific, under `src/Apps/FaunaFinder/FaunaFinder.Client/Components/Species/`. Per repo convention (`docs/creating-components.md`): MudBlazor utility classes only; the only custom CSS is in the design-token file from 2.2 plus a card-grid stylesheet alongside `SpeciesCard.razor.css`.

### `SpeciesEditorialHero.razor` (new)
- Two-column grid (`MudGrid` xs=12 md=8 / md=4).
- Eyebrow "Volume 03 · Living Atlas" — pull volume from a `[Parameter]` (default static).
- Display heading "Species of *Puerto Rico*, catalogued and observed." (italic em on "Puerto Rico").
- Lede paragraph — accept `[Parameter] string Lede`.
- Right column meta strip: `Last sync · {time}`, `{stats.TotalSpecies} records · {stats.MunicipalitiesCovered} municipios`, `Updated daily`. Receives `SpeciesStatsDto`.

### `SpeciesStatsRow.razor` (new)
- 4-column grid backed by `SpeciesStatsDto` from `/wildlife/species/stats`.
- Columns: Total / Endemic / Threatened / Municipios. Each column = label (eyebrow), big number (serif, 2.75rem), sub-line.
- Loading state via the **loading-state pattern** (no `_isLoading` bool; nullable sentinel — see `MEMORY.md` "Loading state pattern").
- Use `IFetch<SpeciesStatsDto>` from `BlazingSingularity.Fetch`.

### `SpeciesFeaturedRow.razor` (new)
- Calls `/wildlife/species/featured`, takes the first 3 cards.
- Renders `SpeciesCard Variant="Featured"` (1 large) + 2 `Variant="Medium"` in a 3-column grid.
- Hide the section if the list is empty (no placeholder noise).

### `SpeciesCard.razor` (rewrite — keep the file)
Replace the row layout entirely. New template:
```razor
<article class="ff-card @VariantClass" @onclick="HandleClick">
    <div class="ff-card-img">
        @if (Species.HasProfileImage) { <img ... /> } else { <fallback /> }
        <span class="ff-card-taxa"><MudIcon Icon="@TaxonIcon" /></span>
        @if (Species.IsEndemic && Variant == SpeciesCardVariant.Featured) { <span class="ff-card-endemic">Endemic PR</span> }
        @if (Species.IucnStatus is { } status) { <span class="ff-card-status status-@status">@status</span> }
    </div>
    <div class="ff-card-body">
        <div class="ff-card-name">@CommonName</div>
        <div class="ff-card-sci">@Species.ScientificName</div>
        <div class="ff-card-foot">
            <span class="muni"><MudIcon Icon="@Icons.Material.Filled.LocationOn" Size="Size.Small" /> @Species.MunicipalityCount municipios</span>
            <span>@FormatLastSeen(Species.LastObservedAtUtc)</span>
        </div>
    </div>
</article>
```
Variants enum: `Default | Featured | Medium`. Icon mapping table (taxon code → MudBlazor icon) lives in a static `TaxonIcons.cs` next to the card.
`SpeciesCard.razor.css` holds: card hover lift, image grayscale-to-color reveal on hover, badge positions, status pill colors (referencing the `--status-*` tokens from 2.2).

### `SpeciesGrid.razor` (new — wraps `EcoDataVirtualizedList`)
- Replaces the inline list usage in `SpeciesList`.
- Renders cards in a CSS grid: `repeat(4, 1fr)` desktop, `repeat(2, 1fr)` ≤ md, 1 col on xs (CSS in `SpeciesGrid.razor.css`).
- Skeletons mirror the card aspect ratio (4:3) so initial load doesn't pop.

### `SpeciesToolbar.razor` (new)
- Search field (`NuiSearchBar` — already exists — keep, but with new placeholder "Search by common name, scientific name, or municipio…").
- Filter button — opens new `SpeciesFilterDialog`.
- Active-filter chip row underneath (Taxon: …, Status: …, Endemic only, etc.). Each chip removable; rebuilds parameters and refreshes the virtualized list.
- Right-aligned counter: "Showing X of {total}".

### `SpeciesFilterDialog.razor` (rewrite)
Sections, in order, matching the mockup:

1. **Taxonomic group** — `MudGrid` of `SpeciesTaxaChip` (4 cols, 8 cards). Each chip = icon + label + count from `SpeciesFacetsDto.Taxa`. Multi-select. `Select all` link.
2. **Conservation status (IUCN)** — `MudGrid` 2-col list of rows: pill + label + count. Multi-select.
3. **Qualifiers** — three checkbox rows: Endemic only / Observed in last 12 months / Has photo (re-labeled from "high-res imagery"). Each shows count from facets.
4. **Minimum municipios present** — `MudSlider` 1..78 bound to `MinMunicipalityCount`.

Footer: `Reset all filters` (text button) / `Apply · {N} results` (filled). Apply triggers refetch with `count` first, label updates live (debounced).

Result type:
```csharp
public sealed record SpeciesFilterResult(
    IReadOnlyList<string> TaxonCodes,
    IReadOnlyList<IucnStatus> IucnStatuses,
    bool IsEndemic,
    bool ObservedRecently,
    bool HasPhoto,
    int MinMunicipalityCount);
```

## 2.4 `Pages/Species.razor` rewrite (list mode)

Replace the list-mode branch with the editorial composition. Keep the detail-mode branch (`Id is not null`) untouched for now — that's a separate redesign.

```razor
@if (Id is null)
{
    <NuiPageLayout HideTitle="true" FullWidth="true">
        <div class="ff-page">
            <SpeciesEditorialHero Stats="_statsFetch?.Data" />
            <SpeciesStatsRow Stats="_statsFetch?.Data" />
            <SpeciesFeaturedRow OnSpeciesClick="NavigateToSpeciesDetail" />
            <SpeciesToolbar Filter="_filter" FilterChanged="OnFilterChanged"
                            SearchText="@_search" SearchTextChanged="OnSearchChanged"
                            Stats="_statsFetch?.Data" Facets="_facetsFetch?.Data" />
            <SpeciesGrid Filter="_filter" Search="@_search"
                         OnSpeciesClick="NavigateToSpeciesDetail" />
        </div>
    </NuiPageLayout>
}
```

State:
- `_statsFetch : IFetch<SpeciesStatsDto>` — fetch on init.
- `_facetsFetch : IFetch<SpeciesFacetsDto>` — refetch when `_filter`/`_search` change (so chip counts reflect current view).
- `_filter : SpeciesFilterResult` — default empty.
- `_search : string?`.

`NuiPageLayout` may need a `FullWidth` parameter (current default constrains via `MudContainer Large`). Verify in `NuiPageLayout.razor`; if absent, add it (Plan 2 task). The mockup uses a 1360px frame, so we can also wrap in `<div class="ff-page">` and let CSS cap width — simpler.

## 2.5 Layout polish

- `MainLayout.razor`: AppBar already uses `Color.Primary`. With the theme change (2.1), the green deepens automatically. Add the `app-bar-blur` class (already there) and remove the `MudDivider` between logo and page title on the species list page (the editorial hero replaces the page-title role) — guard with `IsHomePage || IsSpeciesListPage`.
- Mockup shows the bottom mobile-nav going away on the species page in favor of toolbar reachability — keep current bottom-nav, no mockup parity work.

## 2.6 Acceptance for Plan 2

Manual verification (dev server, browser):
- `/species` renders editorial hero, 4 stats, 3 featured cards, toolbar, 4-column card grid.
- Hover on a card: image fades from 35% grayscale to color, card lifts.
- Status pill matches IUCN code on each card; endemic species in featured row show the brass "Endemic PR" badge.
- Open filter modal: 8 taxa cards with counts, 6 status rows with counts, qualifier checkboxes, slider 1..78. Apply updates the grid + active chips + result counter.
- Search "coqui" matches a Spanish common name; search a municipality name returns species recorded in it.
- Quote `count` from the toolbar matches the filtered grid total.
- Theme primary is `#1f4d3a` everywhere (AppBar, buttons, links).
- Mobile (≤ md): grid collapses to 2 cols, stats wrap to 2x2, hero stacks.

---

## Risk / call-outs

- **IUCN vs NatureServe ranks**: the mockup uses IUCN. The cleanest path is to add an `IucnStatus` column. Mapping `GRank → IucnStatus` is *advisory* (NatureServe uses a finer 1–5 scale that doesn't exactly equal IUCN categories), so seeding via the mapping is acceptable but not authoritative. Document this in the seeder.
- **`OwnsMany.ToJson` queryability**: `s.CommonName.Any(c => EF.Functions.ILike(c.Value, ...))` may not translate in older EF Core versions. Verify against the project's EF Core version (check `FaunaFinder.Client.csproj` / Wildlife project for `Microsoft.EntityFrameworkCore` version) before committing the search-extension code. Fallback: add a denormalized `SearchableNames` column populated on save.
- **Cursor pagination + sort**: enabling `Sort` requires either restricting cursor pagination to `ScientificNameAsc` (with composite `(name, id)` cursor) or accepting that non-default sorts use offset pagination. Keep cursor pagination as-is and treat the toolbar `Sort` button as a follow-up enhancement.
- **`HasHighResImage` filter**: we don't store image dimensions. Recommend re-labeling the qualifier "Has photo" and reusing `HasProfileImage`. If true high-res classification is desired later, add a `ProfileImageWidth` column.
- **Featured row scope**: 3 hand-curated species. Either set `IsFeatured = true` in seed data (preferred) or expose a small admin affordance — out of scope here.
- **No new Observations table**: `LastObservedAtUtc` is a denormalized column. When an observations feature lands, it should update this column (or be replaced by a query). Keep the column semantically optional so older species can render "Verified" instead of "3d ago".
