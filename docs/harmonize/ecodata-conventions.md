# EcoData — Conventions Document (Current State)

How EcoData is actually built, dimension by dimension — the honest current state, compiled from a six-agent full-codebase survey (2026-08-05). Companion documents: `harmony-conventions.md` (the sibling codebase whose conventions we intend to adopt) and `comparison.md`.

Structured to mirror `harmony-conventions.md` section-for-section. Each section separates: **real conventions** (what the code actually agrees on), **splits** (where features/apps diverge), **dead weight**, and **bugs found during the survey**.

## 0. Solution shape

50 csproj (+1 test project) + 1 Zig feature, all net10.0.

```
src/
├── Apps/      EcoPortal.Server/.Client (Blazor WASM) · FaunaFinder.Server/.Client (Blazor WASM)
│              · EcoData.Genomics (Zig binary drop, no source here)
├── Features/  Identity (7 projects) · Organization (6) · Sensors (6+Ingestion) · Wildlife (6)
│              · Locations (6) · Genomics (Zig, 0 .NET) · Common (9 projects)
└── Host/      EcoData.AppHost (Aspire 13.2) · ServiceDefaults · Seeder (Worker) · Aspire.Hosting.Zig
```

- Feature-based modular monolith, acyclic reference graph, no feature depends on an App.
- Both Blazor apps are WASM with `InteractiveWebAssemblyRenderMode(prerender: false)` — DI singletons are per-browser-tab, no prerender hazards.
- **Genomics is a Zig HTTP server** (~1,458 LOC hand-rolled web framework), wired into Aspire via a custom `AddZigApp` resource, run-mode only (`ExcludeFromManifest()`), listed in the slnx as loose files. No contracts, no DB, no client. Outside all .NET conventions; needs a status decision.
- No `Directory.Packages.props`/`Directory.Build.props` — versions hand-pinned per csproj and drifted (EF Design 10.0.1/10.0.3/10.0.5; `Components.Web` 10.0.1/10.0.3/10.0.4).

## 1. Feature-slice architecture

### The documented model vs the six realities

`docs/architecture.md` claims a uniform six-project module with `Application.Server` as "business logic / the migration seam." No feature matches it exactly:

| Feature | Projects | Application.Server | Notes |
|---|---|---|---|
| Identity | 7 | interfaces only (59 LOC), consumed | Extra undocumented `Application` project holds the real service layer (`AuthService`, JWT) |
| Organization | 6 | one 11-LOC interface, **genuinely consumed** cross-feature | No DI class in it |
| Sensors | 6 | **absent** | Has `Ingestion` (Worker SDK) instead — the model for background work |
| Wildlife | 6 | **dead 16-line stub** dragging Identity + Common.Messaging into FaunaFinder.Server | |
| Locations | 6 | absent | Has unique `Helpers` project (26 LOC, consumed by Sensors) |
| Genomics | 0 | — | Zig |

### Real conventions
- **No MediatR, no CQRS, no handlers anywhere.** Dominant pattern: minimal-API endpoint lambda → repository interface, method-injected, `[AsParameters]` Parameter records, `IAsyncEnumerable` returned bare for lists.
- Per-feature `Contracts` project of `sealed record`s: `Dtos/`, `Parameters/`, `Requests/`, `Errors/`, `Events/` (Sensors only).
- Registration is flat and greppable: `Add<X>Database(connectionName)` on `IHostApplicationBuilder` (bodies identical across all 5), `Add<X>DataAccess()`, `Map<X>ApiEndpoints()`, `Add<X>Client(Uri)`. No assembly scanning.
- The two hosts compose different subsets (EcoPortal: all 5 features + auth + messaging + workers, 101-line Program.cs; FaunaFinder: Locations + Wildlife only, no auth, 47 lines).
- Cross-feature seam that works: `IOrganizationPermissionService` and `IUserLookupService`/`ISensorIdentityProviderService` via `Application.Server` interfaces, implemented in the owning feature, consumed cross-feature via DI.
- Async events via Azure Service Bus (`Common.Messaging`, `IMessageBus.PublishEventAsync`/`SubscribeToEventsAsync`), one topic + subscription-per-event-type declared in AppHost referencing contract constants. All three event types are Sensors-owned.

### Splits & violations
- **Sensors orchestration is homeless**: `SensorHealthMonitorWorker`, `NotificationDispatcherWorker`, `ReadingEventLoggerWorker` + `NotificationRoutingService` live in `EcoPortal.Server` (one carries `// TODO: Move to dedicated service`), forcing EcoPortal.Server to reference all 5 Database+DataAccess projects. `Sensors.Ingestion` is the existing model for where this belongs.
- Boundary violations: `Organization.Api → Sensors.DataAccess` (direct using of another feature's repos); `Sensors.Ingestion` injects 7 repositories across 3 features directly; `Organization.DataAccess` repositories call Identity's `IUserLookupService` from inside data access.
- Contracts are not dependency-free: all 5 reference `Common.Pagination`; Wildlife adds `Common.i18n`; four pull OneOf/FluentValidation (largely unused).
- Registration raggedness: Identity and Locations have no endpoint aggregator; `AddWildlifeDataAccess` alone takes `IConfiguration`; `AddWildlifeClient` lacks the `Action<HttpClient>` overload; `AddXApplication`/`AddXApplicationServer`/nothing trichotomy.
- Locations/Organization endpoint files sit at project root (no `Endpoints/` folder).

### Dead weight
- `Wildlife.Application.Server` (16-line no-op with heavy transitive refs), `AddIdentityApplicationServer<T>` (zero call sites).
- `Common.Messaging`: `Endpoints/` (`MapEventStream`/`MapEvent`/`MapCommand`), `Handlers/`, `AddEventHandler`/`AddCommandHandler`, `SendCommandAsync` (throws `NotSupportedException`) — ~1,400 LOC, zero call sites. `SseEventTypes` orphaned (SSE removed in #225).
- `Common.Results` (390-LOC Result monad) — one internal consumer, zero feature/app consumers.

## 2. Data access

### Real conventions (the strongest layer — write these down as-is)
- `public sealed class XRepository(IDbContextFactory<TContext> contextFactory) : IXRepository` — primary ctor, **factory-per-method** (`await using var context = await contextFactory.CreateDbContextAsync(ct)`, 181 occurrences, zero injected DbContexts in repos). No base class, no generic repository, no specification, no unit of work — zero exceptions across 21 repositories.
- **Inline DTO projection into Contracts inside `Select`** — no AutoMapper, no mapper classes, no shared projection expressions (repeated projections are copy-pasted, e.g. the 13-arg `SpeciesDtoForList` appears 4×).
- Returns: `Task<TDto?>` nullable sentinel for single reads; `IAsyncEnumerable<TDto>` + `[EnumeratorCancellation]` for paged lists; `Task<IReadOnlyList<TDto>>` for bounded lists; `Task<bool>` for delete/exists.
- `Guid.CreateVersion7()` ids and `DateTimeOffset.UtcNow` stamped in the repo; `SaveChangesAsync` only inside the method, at most once; no transactions anywhere in the solution.
- Reads take `[AsParameters]`-bound `<Resource>Parameters` records; DTO naming `<X>DtoFor<Purpose>` (ForList/ForDetail/ForCreate/ForUpdate) dominant.
- Files: `Interfaces/I<X>Repository.cs` + `Repositories/<X>Repository.cs`; one `AddScoped` per repo in a per-feature DI extension. Both interface and impl are `public`.

### Splits (decisions Harmonize must make)
- **Pagination: one base type (`CursorParameters(int PageSize = 20, Guid? Cursor = null)`), five behaviors**: cursor direction `Id >` (Locations/Organization/Identity) vs `Id <` (Sensors/Wildlife); Guid operators vs `CompareTo`; `Take(PageSize + 1)` has-more probe (most) vs plain `Take(PageSize)` (**all 6 Organization repos — has-more undetectable**); no clamping anywhere; one in-memory paging implementation (`SurfaceWaterRepository`).
- **Organization buffers every list** into anonymous types, then enriches via cross-module `IUserLookupService` — its `IAsyncEnumerable` is cosmetic.
- Typed errors essentially absent: exactly 1 of 21 repos returns `OneOf` (`SensorRepository.RegisterAsync`); everything else is `null`/`false`/`[]` sentinels or ad-hoc `InvalidOperationException`/`ArgumentException` throws.
- **Four write idioms coexist** (three inside Sensors alone): Add+Save; `AsTracking()` load-mutate-save; untracked load + `Attach` (SensorHealthRepository only); `ExecuteUpdateAsync` (exactly one use; zero `ExecuteDeleteAsync`).
- Write inputs: DTO records vs a Requests record vs loose positional args (Organization) vs a 6-field ValueTuple (`CreateManyAsync`).
- Database enums leak through DataAccess interfaces (Organization, Sensors); Wildlife correctly keeps `IucnStatus` in Contracts.
- `AsNoTracking` sprinkled redundantly (global default is NoTracking) in Locations + 3 Organization queries; 2 `.Include()` in Sensors contradicting the docs.
- Search: escaped `EF.Functions.ILike` (Wildlife only) vs `.ToLower().Contains` (everyone else).
- Identity is the structural outlier: **no repositories at all** — EF queries live in `Identity.Application/AuthService` alongside `UserManager<User>`.
- Multi-query aggregates: `GetStatsAsync` = 6 sequential round-trips, `GetFacetsAsync` = 5 (Wildlife); `SurfaceWaterRepository.GetSummaryAsync` = 5.

### Bugs found
1. **`SensorHealthRepository.ResolveAlertsAsync` never persists** — loads under global NoTracking, no `AsTracking`/`Attach`, mutates, saves → no UPDATE issued; alerts are never resolved.
2. **`SensorRepository.GetSensorCountAsync` applies the cursor to the COUNT** — the reported total shrinks as the user pages (Wildlife's count correctly omits it).
3. Cursor-vs-sort mismatch: `SpeciesRepository` (known, issue #188, commented) and `ReadingRepository` (same bug class, uncommented) — non-default sorts return page 1 forever.
4. `GeoTestEndpoints.cs` (Sensors) does full read+write+SaveChanges directly in an endpoint, returning `StackTrace` in the response body — diagnostic scaffolding that shipped.

## 3. Database

### Real conventions
- **One Postgres server, five physical databases** (database-per-feature, not schema-per-feature; everything in `public`). Local dev = `postgis/postgis:16-3.4` container + pgAdmin; publish = Azure Flexible Server. Custom `WithDropDatabaseCommand()` dashboard command.
- One sealed context per feature, primary ctor, expression-bodied DbSets, config via `ApplyConfiguration(new X.EntityConfiguration())` — **nested `EntityConfiguration` classes inside each entity file**, zero data annotations solution-wide.
- The five shared options are the strongest convention in the repo: `UseSnakeCaseNamingConvention()` + global `QueryTrackingBehavior.NoTracking` + `MigrationsAssembly` + `MigrationsHistoryTable("__ef_migrations_history", "public")` + Aspire keyed data source.
- Entities: `sealed`, `required` scalars including nullable ones, collections `= [];`, `Guid Id` keys (app-assigned v7, never DB-generated), enums stored `HasConversion<string>()` + `HasMaxLength`, `DateTimeOffset` timestamps, `HasMaxLength` on essentially every string, explicit `ToTable("snake_case_plural")` belt-and-braces.
- Cross-feature FKs are soft — bare Guid + index, no constraint, documented by comment. In-feature FKs use explicit `OnDelete` behaviors.
- Migrations: standard EF scaffolding per feature, `<Verb><Noun>` naming, applied **only by `EcoData.Seeder`** (1,128-line idempotent worker; also the only sanctioned cross-database writer — undocumented as such). Apps `.WaitFor(seeder)` in Aspire.

### Splits & oddities
- **Two incompatible registration patterns**: Identity/Organization = `AddPooledDbContextFactory` + scoped bridge (pooled contexts never returned to the pool; Organization's bridge has no justification); Locations/Sensors/Wildlife = `AddDbContextPool` + unpooled `AddDbContextFactory()` piggybacking the same options (two lifetimes, silently different).
- **Wildlife carries PostGIS + NetTopologySuite across four files with zero spatial columns** (copy-paste).
- Locations has no design-time factory (only feature missing one); Wildlife's factory points at the real dev DB (`wildlife` vs everyone else's `<feature>_design`).
- Two `Database` projects (Organization, Wildlife) reference their `Contracts` for an enum; the other three keep enums local.
- Wildlife-only constructs: JSONB owned collections (`OwnsMany(...).ToJson()` for `LocaleValue` i18n), hand-named indexes with three suffix conventions (`_uidx`/`_ix`/`_idx`), filtered indexes, the sole `HasDefaultValueSql`, `...AtUtc` suffix, `= null!` navigations (vs nullable everywhere else), a `bytea` image blob.
- Identity: derives `IdentityDbContext<>`, only inline `OnModelCreating` mapping (renaming ASP.NET Identity tables), out-of-step package version.
- Integration tests bypass the `Add<X>Database` extensions and re-register contexts three different ways, double-registering `OrganizationDbContext` and losing snake_case/NoTracking settings.
- Dead config: `SEED_TEST_DATA=true` set in AppHost Testing env; nothing reads it.

Ranking (cleanest → messiest): Locations, Organization, Sensors, Identity, Wildlife.

## 4. HTTP clients & error handling

### What exists vs what's used
| Piece | LOC | Real consumers |
|---|---|---|
| `Common.Results` (`Result<T>` monad + `Error` + `CommonErrors`) | ~390 | **Zero** (one internal helper file) |
| `Common.Http.Helpers`: `TryGetFromJsonAsync<T>` | — | **Zero call sites** |
| `Common.Http.Helpers`: `QueryStringBuilder` | — | 13 files — genuinely load-bearing |
| `Common.Problems.Contracts`: `ProblemDetail` + `ReadProblemAsync` | 57 | **33 call sites** (Identity/Organization/Sensors) — the one real convention |
| `ValidationProblemDetail` + `ReadValidationProblemAsync` | — | **Zero** — nobody parses the `errors` map |
| Per-feature `Errors/CommonErrors.cs` (Organization, Sensors) | — | ~85% unused; `Success` collides between feature-local and `OneOf.Types` versions |
| `Identity.Contracts/Errors/AuthErrors` | — | Genuinely used server-side, flattened to prose at the HTTP edge |

### Server side
- Minimal APIs, `MapGroup`, static endpoint extensions, `Results<...>` unions mostly, `TypedResults.Problem(detail, statusCode:)` as the dominant error verb (Organization 21×, Sensors 15×, Identity 12×, Locations 6×, **Wildlife 0** — bodiless `NotFound()` only).
- 9 handlers drop to untyped `IResult`/`Results.*`, losing the union.
- **No exception infrastructure**: both apps call `UseExceptionHandler("/Error")` but **no `Error.razor` exists in either app**; in Development there is no handler at all — raw 500s with no problem+json body, which `ReadProblemAsync` then fails on (**it doesn't catch `JsonException`** on non-JSON bodies).

### Client side — five styles coexist
- Uniform mechanics: `AddHttpClient<IFoo, Foo>` per feature, primary-ctor sealed classes, `{Resource}HttpClient` naming, no base class, no shared send helper. Then:
  - **A. Parse to `OneOf<T, ProblemDetail>`** via `ReadProblemAsync` — Organization (14/14 fallible methods — the model), Identity, most of Sensors.
  - **B. Erase to `null`/`[]`/`0`** — Wildlife (0/11 methods can report failure), Locations, pockets of Sensors *in the same class as style A*. A 500 is indistinguishable from empty data; failed counts render as "0".
  - **C. `EnsureSuccessStatusCode()` throw** — 3 Sensors methods; caught once with a blind bare `catch`, uncaught elsewhere (unhandled in the circuit).
  - **D. Bare try/catch swallow** — Identity `GetCurrentUserAsync`.
  - **E. Streaming with no error channel** — all 24 `IAsyncEnumerable` client methods use `GetFromJsonAsAsyncEnumerable(...)!`; non-2xx throws mid-enumeration inside virtualized lists, unhandled.
- App-local clients bypass features: `EcoPortal.Client/Services/{LocationHttpClient,DataSourceHttpClient}` duplicate feature clients in style B.
- **No resilience for the browser**: `AddStandardResilienceHandler` lives in ServiceDefaults (server-only); WASM clients have zero retry/timeout/401-handler/DelegatingHandler. No Polly.

### Validation — three unreconciled mechanisms
1. Identity: DI-registered FluentValidation run in `AuthService`; failures flattened with `string.Join(", ")` into `Problem.detail` — **field names lost**.
2. Sensors: inline `new XValidator()` in 2 endpoints returning proper grouped `TypedResults.ValidationProblem` — **then discarded by its own client**, which parses into `ProblemDetail` (no `errors` property, null `detail`) and shows a hardcoded fallback message.
3. Organization/Locations/Wildlife: no validation at all (Wildlife references FluentValidation, defines zero validators).
- Client-side pre-validation in exactly one place (LoginPage, shows only `Errors[0]`).
- Display: form pages bind `_errorMessage` → MudAlert; dialogs/lists use `Snackbar.Add(problem.Detail ?? "...")`. No per-field binding anywhere.

Ranking: Organization best; Sensors richest/least consistent; Identity good-intent-lossy-boundary; Locations consistently bad; Wildlife worst (FaunaFinder has **no error UI surface at all** — a dead database looks like an empty result set).

## 5. Blazor components

### Inventory
138 `.razor` solution-wide: EcoPortal.Client 96 (11,094 LOC), FaunaFinder.Client 24 (3,033 LOC), NativeUi 10, Pagination.Blazor 3, Maps 2.

| Library | LOC | Consumers |
|---|---|---|
| `EcoData.NativeUi` (9 Nui components + 2 singleton managers + sticky-search JS) | ~1,306 | Both apps — but **6 of 9 components have zero EcoPortal usages** |
| `EcoData.Common.Pagination.Blazor` (`EcoDataVirtualizedList` + `EcoDataVirtualizedGrid`) | 433 | List: EcoPortal only (10 sites). **Grid: zero consumers — 100% dead** |
| `EcoData.Common.Maps` (`NuiMap` + controller, Mud-free, "Experimental") | 608 | FaunaFinder only |
| `EcoData.Common.i18n` | 199 | FaunaFinder + Wildlife + Seeder |

### The forks
- **Three near-identical virtualized lists** (~600 LOC triplicated: same `_cachedItems`/`_generation`/`async void Refresh()` algorithm). `EcoDataVirtualizedGrid` doesn't even grid; `NuiVirtualizedGrid` is the only real grid. Collapse to one component with a `Columns` parameter, delete two.
- **Two Leaflet stacks** (EcoPortal's `LeafletMap`+`LeafletMapService`+`map.js` vs Common.Maps' `NuiMap` ES-module controller) plus six bespoke map wrapper components.
- **Two search bars**: EcoPortal's `SearchHeader` wraps `NuiSearchBar` with sticky disabled, reimplements the same sticky JS, and imports `./js/stickySearch.js` **which doesn't exist in EcoPortal's wwwroot** (silent no-op). Nobody uses NuiSearchBar's own sticky feature.
- **Two `MainLayout`s** in copy-paste lineage (same provider block, same comment structure, same helper trio, near-identical `.razor.css`); FaunaFinder's `app.css` is a superset fork of EcoPortal's; two structurally near-identical theme classes with different names (`AppThemes.Azure` ocean blue vs `FaunaFinderTheme.Default` pine).
- Small clones: `StatCard` vs `StatsHero`; `ActionListItem` vs `NuiListItem`; `PageHeader` vs `NuiSectionHeader`; homemade `Tabs`/`Tab` (cascades `this`) instead of `MudTabs`; EcoPortal hand-rolls empty states in 6 files while `NuiEmptyState` sits unused by it.

### The philosophical split
- **EcoPortal is MudBlazor-first**: 1,330 Mud tags (13.9/file), layout built from `MudStack`/`MudPaper`/`MudText`.
- **FaunaFinder is semantic-HTML + design tokens**: 177 Mud tags (7.4/file), Mud used mostly for icons/skeletons/menus, layout from `<article class="ff-card">` + `fauna-tokens.css` (the only token layer in the solution; EcoPortal has none).
- Organization: EcoPortal is feature-sliced (`Features/<F>/{Components,Dialogs,Pages,Services}` — matches docs) with a 21-component legacy flat bucket; FaunaFinder is type-first, no feature folders, 4 dual-route mega-pages (`@page "/species"` + `@page "/species/{Id:guid}"` with `Id is not null` branching), plus an empty `_Components.cs` marker class to make a namespace exist.
- Naming: `*Page` suffix + `BlazorStaticNavigation` generated `Path` constants (EcoPortal) vs bare nouns + hard-coded hrefs (FaunaFinder).

### Real shared conventions (keep)
- Zero `.razor.cs` code-behind in apps (only generic components in shared libs); inline `@code` blocks.
- `[Parameter, EditorRequired]`; `Class` parameter merged via `GetContainerClass()` list-join (copy-pasted 6×); enum variants → CSS classes via switch expressions.
- `EventCallback` invoked through private `HandleClick` wrappers; `IsClickable => OnClick.HasDelegate`.
- `RenderFragment` slot quartet on lists: `ItemTemplate`/`PlaceholderTemplate`/`LoadingTemplate`/`EmptyTemplate`.
- JS interop: `IAsyncDisposable` + ES-module import in `OnAfterRenderAsync(firstRender)` + `DotNetObjectReference` + `catch (JSDisconnectedException)`.
- **Loading = `MudSkeleton` shaped like the content** (156 uses) — the one place docs and reality agree.
- EcoPortal dialog convention: `IDialogService.ShowAsync<T>` + `DialogParameters<T>` + generic `ConfirmDialog` for 6 destructive-action sites.

### Docs vs reality
`docs/creating-components.md` rule #1 "No custom CSS files" vs **7,330 LOC of scoped CSS across 68 `.razor.css` files** (EcoPortal 4,838 across 40; five files larger than their page — `OrganizationDetailsPage` 630 LOC page / 704 LOC css). Both FaunaFinder migration docs assert "MudBlazor utilities only" for work that shipped 19 CSS files, and cite components (`EcoDataVirtualizedList`) FaunaFinder never used. Neither app has a working dark-mode toggle despite both shipping dark palettes.

## 6. Reactivity / state management

### Packages
Both clients: **the former reactivity package at 1.0.0-beta.5** (its beta.6 restored locally, never adopted; both clients have since migrated to Tempest.Blazor) + MudBlazor 9.1.0. EcoPortal adds `BlazorStaticNavigation`, auth packages, `IMemoryCache`.

**The sharpest finding**: both apps use disjoint subsets of the same package. FaunaFinder uses only `Fetch<T>`; EcoPortal uses all four mechanisms (`Fetch<T>`, `BackgroundFetch<T>` — one site, `[RelayCommand]`, `[Signal]`). `[Signal]` is a second, parallel reactivity system: `Signal<T>` objects passed **by reference as component parameters**, mutated by the child, observed by the parent via `OnChange` — inverting Blazor's `EventCallback` flow.

### Mechanism counts: FaunaFinder 9, EcoPortal 13
Shared five: `Fetch<T>` with mandatory `() => InvokeAsync(StateHasChanged)` callback; NativeUi singleton managers (`Action? OnStateChanged`); virtualized-grid internal caches (`_generation` counter + `async void Refresh()`); shadow-field dirty checking; raw fields + manual `StateHasChanged`.
FaunaFinder-only: `ILocalizer.LanguageChanged` via base class AND hand-subscribed per page AND a parallel `CascadingValue<LocaleContext>` — **three localization channels firing on one event** (`LocalizedComponentBase` now has 11 inheritors; some components use base class + cascade simultaneously). EcoPortal-only: `[RelayCommand]`/`[Signal]`, `AuthStateService`/`NotificationService` `Action?` events, `AuthenticationStateProvider` bridge, `IMemoryCache` service (no change notification), task-memoization permission cache (caches faulted tasks forever, no invalidation event), homemade `Tabs` cascading `this`, URL-derived `TabNavigationService`.

- `StateHasChanged`: **134 occurrences across 55 files** — structural, not sloppy: `Fetch<T>`'s constructor requires the re-render callback. Any "no hand-written StateHasChanged" rule is incompatible with `Fetch<T>` as shaped.
- Cache invalidation is always "call `.Refresh()` on the right `@ref`" — `_virtualizedList?.Refresh()` after every mutation, `@ref` required on nearly every list page.
- **No real-time push exists**: SSE removed (#225), messaging moved to Service Bus (#223) which terminates in server workers; zero SignalR/WebSocket/EventSource anywhere. `NotificationService.OnNotificationReceived` is preserved-for-compatibility, never fired, yet still subscribed by two components (issues #222/#224 track the future bridge).

### Loading/error state adherence
Four loading conventions coexist: `Fetch.IsLoading` tri-state (`_fetch?.IsLoading is not false`); list templates (consistent where used); nullable-collection sentinel; and hand-rolled `bool _isLoading` + try/finally **still alive in 7 EcoPortal components** (FaunaFinder has zero). Error: newest EcoPortal pages use `Fetch.OnChange` + `if (IsError) Snackbar.Add(...)` copy-pasted 3× per page; mutations use `result.Switch(..., problem => Snackbar)` consistently; **`IFetch.IsError` is never rendered inline**; FaunaFinder collapses failures into not-found/empty states.

### Defects found
- Leaked `Fetch` disposables: `OrganizationDetailsPage` disposes 1 of 4; `SensorsPage`/`DataHubPage`/`SurfaceWaterDashboardPage` construct 3 each with no disposal. Undisposed per-load CTS in `SensorReadingsPreview`.
- 7 `async void` sites — including the documented public list-refresh API (`Refresh()` on all three virtualized components).
- Unguarded lifecycle refetches: `SensorSubscriptionToggle` double-loads on mount; `SensorReadingsList.OnParametersSet` blows the cursor cache on any ancestor re-render.
- `AccountPage` subscribes `StateHasChanged` raw (no `InvokeAsync`) — the only unmarshalled subscriber.
- **Latent bug**: `MunicipalityList` sort menu mutates `_sort` but never recomputes — order doesn't change until the parent happens to re-render.
- Shipped debug code in an auth component: `Console.Write("hello")` + mangled `StateHasChanged\n();` in `RequireOrganizationPermission`.
- One surviving ViewModel (`OrganizationDetailsViewModel`) contradicting the intended no-viewmodels direction.
- Positive: **zero unbalanced event subscriptions** in either app — every `+=` has its `-=`. The hygiene problem is `IDisposable` objects and `async void`, not dangling handlers.

## 7. Meta-observations

1. **The philosophies already match Harmony in the core**: vertical slices, no mediator, endpoint-lambdas → repositories, inline DTO projection, flat greppable registration, soft cross-feature FKs, `required`-on-nullable entity modeling, string enums, GUIDv7, `DateTimeOffset`. The difference is not philosophy — it is **follow-through and enforcement**.
2. **Docs are aspirational, not authoritative.** Every dimension has documented rules the code contradicts (no-custom-CSS vs 7,330 LOC; no-Include vs 2; Application.Server-as-logic-layer vs interface bags/stubs; "never DB from endpoints" vs GeoTestEndpoints; stale system-design diagram). Nothing states Harmony's meta-rule that docs win and code gets fixed.
3. **Abstractions are built speculatively, then bypassed.** Result monad (0 users), ValidationProblemDetail (0 users), messaging command/handler layer (0 users), SseEventTypes (orphaned), per-feature error records (~85% unused), LocalizedComponentBase (written to kill a pattern that then continued alongside it).
4. **Duplication here is unmanaged fork-drift, not Harmony's deliberate duplication**: three virtualized lists, two map stacks, two search bars, two layouts, two themes, two app.css files, duplicated per-feature CommonErrors — each pair diverging silently.
5. **The two apps are two architectures** sharing packages: MudBlazor-first vs token-CSS-first; feature-sliced vs type-first; 31 pages vs 4 mega-pages; disjoint shared-library and reactivity-package subsets.
6. Tests exist (integration tests for Auth/Municipality/Organization/Sensors) but bypass the very registration conventions they should exercise; Wildlife/FaunaFinder have zero coverage.
