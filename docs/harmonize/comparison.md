# Harmonize — Comparison: Harmony vs EcoData

Third document of the Harmonize set. Companions: `harmony-conventions.md` (target conventions) and `ecodata-conventions.md` (current state). This is Phase 1 analysis only: similarities, differences, and root causes — no implementation plan, no task list.

**Source of truth for this document is the two companion documents**, both compiled from full-codebase surveys on 2026-08-05. Section numbers below (e.g. "Harmony §2") refer to sections in those documents.

---

## 1. Executive summary

The headline finding is not that EcoData needs a new architecture. It already has Harmony's architecture in outline: a feature-based modular monolith with vertical slices, no mediator, endpoint lambdas calling flat hand-written repositories, inline DTO projection, GUIDv7 ids stamped in repositories, soft cross-feature FKs, string-persisted enums, `DateTimeOffset` everywhere, and flat greppable registration. EcoData's own conventions document says it plainly (EcoData §7.1): *"The difference is not philosophy — it is follow-through and enforcement."*

Where the codebases actually diverge, it is almost always one of three failure modes on EcoData's side, and it is worth distinguishing them because they call for different responses:

1. **Same idea, N implementations.** One pagination base type, five cursor behaviors. One "client parses problem details" idea, five client error styles. Three virtualized lists with the same algorithm. Four write idioms (three inside one feature). Two DbContext registration patterns.
2. **Speculative abstraction, never adopted.** A 390-LOC `Result<T>` monad with zero consumers; `ValidationProblemDetail` with zero parsers; a ~1,400-LOC messaging command/handler layer with zero call sites; a dead `EcoDataVirtualizedGrid`; 6 of 9 NativeUi components unused by EcoPortal. Harmony's rule is the opposite: projects and abstractions are added "only when earned."
3. **Genuinely different requirements.** EcoData has two apps with different design philosophies, multiple hosts, Azure Service Bus messaging, i18n, PostGIS, integration tests, and a Zig service. Harmony is a single web server, synchronous, English-only, non-spatial, untested. These are not flaws to fix by copying Harmony — they are decisions Phase 2 must make explicitly (Section 10).

The meta-difference underneath all of this (Section 9): Harmony treats its docs as source of truth and fixes code that drifts; EcoData's docs are aspirational and every dimension has documented rules the code contradicts. Harmony duplicates deliberately and locally; EcoData duplicates by unmanaged fork-drift. That meta-habit, more than any single convention, is what makes Harmony read cleaner.

Finally, the survey found real bugs in EcoData that any harmonization work will trip over (never-persisting alert resolution, cursor-corrupted counts, cursor-vs-sort mismatches, leaked `Fetch` disposables, a shipped `StackTrace`-returning test endpoint). They are catalogued in `ecodata-conventions.md` §2/§6 and are not re-litigated here, but several exist precisely *because* a convention had five implementations instead of one.

---

## 2. Shared philosophies (genuine agreement — verified in both documents)

Credit where due. Each row below is asserted in **both** documents, not inferred.

| Philosophy | Harmony | EcoData |
|---|---|---|
| Feature-based modular monolith, vertical cut first | §0 | §0 (acyclic graph, no feature depends on an App) |
| No MediatR, no CQRS, no handlers, no command/query classes | §1 ("no domain model" either) | §1 ("No MediatR, no CQRS, no handlers anywhere") |
| Use case = minimal-API endpoint lambda → repository, method-injected | §1 | §1 (dominant pattern) |
| `[AsParameters]` Parameter records for reads | §1 | §1, §2 |
| Flat hand-written repositories; no generic `IRepository<T>`, no specification pattern, no unit of work, no base class | §2 | §2 ("zero exceptions across 21 repositories") |
| Inline DTO projection into Contracts inside `Select`; no AutoMapper, no mapper classes, no shared projection expressions | §2 | §2 |
| `IAsyncEnumerable<TDto>` + `[EnumeratorCancellation]` for streamed lists | §2 | §2 |
| Cursor pagination keyed on GUIDv7 ids | §2 (uniform 5-step shape) | §2 (`CursorParameters` base — five behaviors, see §4 below) |
| `Guid.CreateVersion7()` app-assigned inside repositories, never DB-generated | §2 | §2, §3 |
| Per-feature Contracts project of `sealed record` DTOs with `Dtos/`, `Parameters/`, `Errors/` folders | §1 | §1 |
| Flat, greppable registration; no assembly scanning, no `IModule` reflection | §1 | §1 ("No assembly scanning") |
| Cross-feature calls via DI-injected `Application.Server` interfaces implemented in the owning feature | §1 | §1 (`IOrganizationPermissionService`, `IUserLookupService` — "the seam that works") |
| Context-per-feature, sealed, primary ctor, expression-bodied DbSets | §3 | §3 |
| Entities: `sealed`, **`required` on every mapped scalar even when nullable**, collections `= [];` | §3 | §3 |
| Enum values persisted as strings with max lengths (mechanism differs — see §5) | §3 | §3 (`HasConversion<string>()` + `HasMaxLength`) |
| `DateTimeOffset` for all timestamps | §3 | §3 |
| `HasMaxLength` on essentially every string; zero data annotations | §3 | §3 |
| Soft cross-feature FKs: bare `Guid` + index, no constraint, documented by comment | §3 | §3 |
| Migrations per feature, `<Verb><Noun>` naming | §3 | §3 |
| Typed `HttpClient` per feature; no base class, no shared `SendAsync<T>` helper | §4 | §4 |
| A tiny problem-details record + parse helper as the transport-error vocabulary | §4 (`ProblemDetailsDocument`/`ProblemDetailsParser`) | §4 (`ProblemDetail`/`ReadProblemAsync`, 33 call sites — "the one real convention") |
| `TypedResults.Problem(detail, statusCode:)` as the dominant server error verb | §4 | §4 (54 sites across four features) |
| No home-grown `Result<T>` in actual use | §4 (explicit rule) | §4 (`Common.Results` exists but has **zero** consumers — dead, so practice agrees) |
| Sealed classes + primary constructors as the default shape | §7 | §2, §4 (repos and clients) |
| Smart pages / dumb shared components; no viewmodel layer for page state | §5, §6 | §5, §6 (one surviving `OrganizationDetailsViewModel`, acknowledged as contradicting direction) |
| Component reactivity from a single third-party attribute/state library, not Fluxor/Redux | §6 (Tempest) | §6 (the legacy reactivity package) |
| Loading states as skeleton/sentinel idioms, not a global spinner framework | §5 | §5 (`MudSkeleton` shaped like content, 156 uses — "the one place docs and reality agree") |

Two of these deserve emphasis because they are easy to miss when cataloguing EcoData's flaws:

- **EcoData's data-access layer is described in its own document as "the strongest layer — write these down as-is"** (EcoData §2). The factory-per-method discipline (181 occurrences, zero injected DbContexts, zero deviations across 21 repositories) is *more* uniform than some of Harmony's own layers.
- **EcoData's five shared database options** (snake_case + global NoTracking + migrations assembly + history table + Aspire keyed data source) are called "the strongest convention in the repo" (EcoData §3). EcoData is fully capable of enforcing a convention when it writes one down as a single reusable shape.

---

## 3. Feature-slice architecture

**Same:** modular monolith, per-feature Contracts/Database/DataAccess/Api projects, endpoint-lambda use cases, flat registration composed by the host, cross-feature `Application.Server` interface seam, hosts composing feature subsets via `Add<X>...`/`Map<X>...` calls.

**Different:**

| Dimension | Harmony | EcoData |
|---|---|---|
| Anatomy uniformity | 6–8 projects, added "only when earned"; docs match code | Documented six-project model that **no feature matches exactly** (EcoData §1 table): Identity has an undocumented `Application`, Sensors swaps `Application.Server` for `Ingestion`, Wildlife's is a dead 16-line stub, Locations has a one-off `Helpers` |
| Slice privacy | Compiler-enforced: `Database`/`DataAccess`/`Internal` referenced by nothing outside the slice; `Api` is a leaf | Violated in three places: `Organization.Api → Sensors.DataAccess`; `Sensors.Ingestion` injects 7 repositories across 3 features; `Organization.DataAccess` calls Identity's `IUserLookupService` from inside data access |
| Contracts purity | Leaf, zero project refs (some take only FluentValidation) | All 5 reference `Common.Pagination`; Wildlife adds `Common.i18n`; four carry OneOf/FluentValidation largely unused |
| Background work | `<Feature>.Internal` services registered via opt-in extensions; "moving to a worker host is a Program.cs change" | `Sensors.Ingestion` is the model — but three Sensors workers + a routing service are **homeless in `EcoPortal.Server`** (one carries `// TODO: Move to dedicated service`), dragging all 5 Database+DataAccess refs into the host |
| Cross-feature async | None — deliberately sync-only, `Application.Server` documented as the future swap point | Real: Azure Service Bus (`Common.Messaging`), topic + subscription-per-event-type, all three events Sensors-owned |
| Registration signatures | Uniform `Add<X>Feature()` + `Map<X>()` | Ragged: `AddWildlifeDataAccess` alone takes `IConfiguration`; `AddWildlifeClient` lacks the `Action<HttpClient>` overload; Identity/Locations have no endpoint aggregator; `AddXApplication`/`AddXApplicationServer`/nothing trichotomy |

**Root cause:** overwhelmingly *same idea, incomplete follow-through*. The `Application.Server` seam exists and works in EcoData (Organization, Identity) — it just also exists as a dead stub (Wildlife), is absent (Sensors, Locations), and is bypassed by direct DataAccess references where it would have been inconvenient. One genuine philosophical difference: EcoData has real async messaging and multiple hosts, which Harmony explicitly does not ("Commands/Events not yet introduced", Harmony §1). That is a capability gap in *Harmony*, and Section 10 treats it as such.

Note also: both documents describe drift honestly. Harmony has its own known violations (`Guilds.Application → Music.Contracts`; the Gaming slice outlier) — the difference is that Harmony's doc marks them "known drift (don't copy)" while EcoData's violations were undocumented until the survey.

---

## 4. Data access

**Same:** flat sealed repositories with primary ctors, inline projection, no mapping layer, `IAsyncEnumerable` streams, GUIDv7 + timestamps stamped in the repo, Parameter records for reads, one DI registration per repo, `Music`/`Guilds`-style file-per-resource layout.

**Different:**

| Dimension | Harmony | EcoData |
|---|---|---|
| Context acquisition | DbContext injected via primary ctor | `IDbContextFactory`, `await using` per method — 181 occurrences, zero exceptions. **EcoData's most uniform habit, and it directly conflicts with Harmony's shape** (open question, §10) |
| Visibility | `internal sealed`, only interface public | Both interface and impl `public` |
| Fallible reads | `Task<OneOf<TDto, NotFound>>` uniformly | `Task<TDto?>` nullable sentinel; exactly 1 of 21 repos returns `OneOf` |
| Errors | `OneOf` arms + plain error records in `Contracts/Errors/`; never exceptions | `null`/`false`/`[]` sentinels plus ad-hoc `InvalidOperationException`/`ArgumentException` throws |
| Writes | `ExecuteUpdateAsync`/`ExecuteDeleteAsync` preferred (24 usages) | **Four idioms coexist** (three inside Sensors alone); exactly one `ExecuteUpdateAsync`, zero `ExecuteDeleteAsync`. The `Attach` variant and the NoTracking-load variant produced bug #1 (alerts never resolved) |
| Write inputs | `<Verb><Resource>Dto`; "a Parameter never carries data to be written" | DTOs vs Requests records vs loose positional args vs a 6-field ValueTuple |
| Pagination | One 5-step shape in every list read, `Math.Clamp(..., 1, 100)`, `DefaultTake` const | One base type, **five behaviors**: `Id >` vs `Id <` direction, operator vs `CompareTo`, `PageSize + 1` probe vs plain `Take` (all 6 Organization repos — has-more undetectable), no clamping anywhere, one in-memory pager |
| Tracking hygiene | Zero `AsNoTracking`, zero `Include` — projections make both moot | Global NoTracking default *plus* redundant sprinkled `AsNoTracking`, plus 2 `Include`s contradicting its own docs |
| Time | `TimeProvider` injected, never `DateTime.UtcNow` | `DateTimeOffset.UtcNow` inline |
| Variant methods | Banned (`GetTopArtists` forbidden — Parameter carries sort enum) | Present; cursor-vs-sort mismatch bugs (#188 + one uncovered twin) live exactly in variant-sort methods |
| Query composition seam | `<Resource>Queries` static extension class, added only when a filter repeats | Repeated projections copy-pasted (13-arg `SpeciesDtoForList` ×4) with no seam — note MEMORY.md's standing rule *against* shared projection helpers, which matches Harmony's convention |
| Structural outliers | Gaming (SQLite/desktop), documented as drift | Identity has **no repositories at all** — EF queries in `AuthService`; Organization buffers lists into anonymous types for cross-module enrichment, making its `IAsyncEnumerable` cosmetic |

**Root cause:** *same philosophy, five implementations* — the purest example in either codebase is EcoData's pagination: everyone agreed on "keyset cursor over v7 ids," nobody agreed on direction, probe, or clamping, and two of the four surveyed bugs (shrinking counts, page-1-forever sorts) fell out of that gap. The factory-vs-injected-context difference is the exception: two genuinely different, internally consistent conventions (Section 10).

Why Harmony reads cleaner here: a reader who has seen one Harmony list read has seen all of them — the 5-step shape is load-bearing documentation. In EcoData a reader must re-derive each repository's cursor semantics, and the has-more probe's absence in Organization is invisible until a UI pager misbehaves.

---

## 5. Database

**Same:** Postgres via Aspire (Azure Flexible Server in publish), context-per-feature, sealed entities with `required`-on-nullable, `= [];` collections, app-assigned v7 keys, string-persisted enum values with lengths, `HasMaxLength` everywhere, zero data annotations, soft cross-feature FKs, `<Verb><Noun>` migrations, design-time factories (EcoData missing one: Locations).

**Different:**

| Dimension | Harmony | EcoData |
|---|---|---|
| Physical layout | **One database, schema-per-feature**, per-schema `__EFMigrationsHistory` | **Five databases, everything in `public`** (database-per-feature) |
| Naming | PascalCase EF defaults — "no snake_case naming convention" (explicit) | `UseSnakeCaseNamingConvention()` — part of "the strongest convention in the repo" |
| Connection plumbing | One shared `NpgsqlDataSource`, each feature `AddDbContext` onto it | Aspire keyed data source per feature database; **two incompatible registration patterns** (`AddPooledDbContextFactory` + scoped bridge vs `AddDbContextPool` + unpooled factory piggyback) |
| Tracking default | EF default (tracking); irrelevant because projections | Global `QueryTrackingBehavior.NoTracking` |
| Entity configuration | 100% inline `OnModelCreating`, one lambda block per entity; zero `IEntityTypeConfiguration` | Nested `EntityConfiguration` classes inside each entity file via `ApplyConfiguration` — a *different single convention*, uniformly applied |
| Enums in Database projects | **None** — columns are plain strings "so the schema reads plainly" | Real enums + `HasConversion<string>()`; two Database projects reference Contracts to get the enum, three keep it local (and enums leak through DataAccess interfaces, EcoData §2) |
| Index discipline | Auto-named; **a comment on every index naming the query it serves** | Wildlife hand-names with three suffix conventions; no query-comment habit |
| Advanced column features | Zero owned types, converters, JSON columns, query filters | Wildlife: JSONB `OwnsMany(...).ToJson()` for i18n `LocaleValue`, filtered indexes, `HasDefaultValueSql`, `bytea` blob, `= null!` navigations |
| Migration application | Sequentially at startup in every environment (documented single-replica assumption, `MinReplicas=MaxReplicas=1`) | Applied **only by `EcoData.Seeder`** (1,128-line idempotent worker, also the only sanctioned cross-database writer — undocumented as such); apps `.WaitFor(seeder)` |
| Spatial | None | `postgis/postgis:16-3.4` container; Wildlife carries PostGIS + NetTopologySuite across four files **with zero spatial columns** (copy-paste); a shipped `GeoTestEndpoints.cs` |

**Root cause:** mixed. Configuration style (inline vs nested classes) and naming (PascalCase vs snake_case) are *genuinely different conventions, each uniformly applied* — EcoData is not sloppy here, it is differently principled, and adopting Harmony's choice would mean churning every table name or accepting a permanent visible difference. Database-per-feature vs schema-per-feature is likewise a real architectural decision, not drift (Section 10). The registration split (two pooling patterns, pooled contexts never returned to the pool) and Wildlife's accumulated one-offs are follow-through failures. PostGIS-with-no-spatial-columns is speculative abstraction in database form.

Why Harmony reads cleaner here: mostly the index-comment habit and the single connection story. EcoData's entity modeling itself is essentially at parity — its own doc ranks Locations/Organization as clean.

---

## 6. HTTP clients & error handling

This is the dimension with the largest gap, and also the clearest case of "EcoData already contains Harmony's answer, plus four others."

**Same:** typed client per feature, sealed + primary ctor, no base class, no shared send helper, a null-returning problem-details parse helper as the core vocabulary, `TypedResults.Problem` server-side, `Results<...>` unions mostly, no Polly/resilience on clients, per-feature error records, FluentValidation as the (nominal) validation library, errors shown at point of display via alert/snackbar with no central message registry.

**Different:**

| Dimension | Harmony | EcoData |
|---|---|---|
| Client error contract | One: `OneOf<T, RequestFailed>`, clients **never throw**, ~8 deliberately repeated lines per method | **Five coexisting styles** (EcoData §4 A–E): OneOf+ProblemDetail (Organization 14/14 — effectively Harmony's style), null/`[]`/`0` erasure (Wildlife 0/11 methods can report failure), `EnsureSuccessStatusCode` throws, bare try/catch swallow, and 24 streaming methods with **no error channel at all** |
| Parser robustness | `ProblemDetailsParser` returns `null` on wrong content type, swallows `JsonException` — "parsers only, no ensure/throw policy" | `ReadProblemAsync` does **not** catch `JsonException`, so a non-JSON 500 breaks the parser that was supposed to handle it |
| Transport-generic error | `RequestFailed(StatusCode, Message?)`, status 0 = never reached server | None — nearest equivalents unused (`Result<T>` 0 consumers, ~85% of per-feature `CommonErrors` unused, `Success` name collides with `OneOf.Types.Success`) |
| Exception middleware | Deliberately none; ASP.NET default | Worse than none: both apps `UseExceptionHandler("/Error")` with **no `Error.razor` in either app**; Development has raw 500s with no problem+json body |
| Client middleware | Exactly one `DelegatingHandler` (401 → login, with auth carve-out) | Zero DelegatingHandlers in WASM; `AddStandardResilienceHandler` exists but is server-only ServiceDefaults |
| Validation | One mechanism: inline `new XDtoValidator()` on both sides, never DI; server errors → `errors` map → flat list in one alert | **Three unreconciled mechanisms**: DI-registered (Identity, field names lost via `string.Join`), inline returning proper `ValidationProblem` (Sensors — **then discarded by its own client**), none (Organization/Locations/Wildlife) |
| Feature clients respected | Yes | App-local clients (`EcoPortal.Client/Services/{Location,DataSource}HttpClient`) duplicate feature clients in the worst style |

**Root cause:** *same idea, five implementations* — with the important nuance that Organization's style A is essentially Harmony's convention already working at full coverage, so "adopt Harmony here" largely means "make the rest of the codebase do what Organization does." Layered on top: two textbook speculative abstractions (`Common.Results`, `ValidationProblemDetail`) that were built as the intended unification and then bypassed by every feature.

Why Harmony reads cleaner here: a single error vocabulary means the *UI* can have a single failure idiom. EcoData's doc notes the end-user consequence directly: in FaunaFinder "a dead database looks like an empty result set" — that is style B's erasure surfacing as product behavior.

---

## 7. Blazor components

**Same:** inline `@code` (zero code-behind in app pages, both codebases), smart pages owning data-load/error/markup with dumb shared components, `MudBlazor` as the component base, service-driven dialogs + snackbar toasts + generic confirm, skeleton/sentinel loading idioms, `RenderFragment` template slots on lists, disciplined JS interop (`IAsyncDisposable` + ES-module import in `OnAfterRenderAsync` + `JSDisconnectedException` catch), empty states as literal sentences.

**Different:**

| Dimension | Harmony | EcoData |
|---|---|---|
| Wrapper layer | **Zero wrappers**; `Harmony.Web.Shared` intentionally empty; whole web UI = 36 `.razor` files | `EcoData.NativeUi` (~1,306 LOC, 9 components — **6 unused by EcoPortal**), Pagination.Blazor (grid 100% dead), Maps, plus per-app clones of NativeUi components (`ActionListItem` vs `NuiListItem`, hand-rolled empty states in 6 files while `NuiEmptyState` sits unused) |
| UI in slices | Feature slices contain **zero UI** | Same — EcoData features also ship no UI (shared libs are Common, not features). Genuine agreement worth noting |
| CSS strategy | One theme class (~75 lines) + **one global `app.css`** (~2,200 LOC BEM-ish); no CSS isolation, zero `.razor.css` | **68 `.razor.css` files, 7,330 LOC** (five files larger than their page), two forked `app.css`, two near-identical theme classes, one token layer (`fauna-tokens.css`) in one app only |
| Design philosophy | One: MudBlazor-first + utility classes | **Two apps, two architectures**: EcoPortal MudBlazor-first (13.9 Mud tags/file), FaunaFinder semantic-HTML + tokens (7.4/file) |
| Page organization | Attribute-routed pages, 400–600 lines, one style | EcoPortal feature-sliced folders (+ 21-component legacy flat bucket); FaunaFinder type-first with 4 dual-route mega-pages and an empty `_Components.cs` namespace marker |
| Extraction bar | "Genuine reuse across 2+ pages, never decomposition-for-its-own-sake"; helpers duplicated cheerfully | Shared libraries built ahead of demand, then *not* used by the app they were built for; meanwhile real duplication (three virtualized lists ~600 LOC triplicated, two Leaflet stacks, two search bars — one importing a JS file **that doesn't exist**, silent no-op) went unshared |
| Component communication | Tempest event bus, nested record contracts; 3 `EventCallback`s and 1 `CascadingParameter` app-wide | `EventCallback` wrappers (fine), plus homemade `Tabs` cascading `this`, plus `[Signal]` by-reference parameters (see §8, Reactivity) |
| Docs vs reality | Docs match; drift marked | `creating-components.md` rule #1 "No custom CSS files" vs 7,330 LOC of it; migration docs assert "MudBlazor utilities only" for work that shipped 19 CSS files and cite components never used; both apps ship dark palettes, **neither has a working dark-mode toggle** |

**Root cause:** three overlapping causes. The virtualized lists / maps / search bars / layouts / themes are *unmanaged fork-drift* — copy-paste lineages diverging silently. NativeUi and Pagination.Blazor are *speculative abstraction* — the shared library existed, and pages were still hand-rolled past it. The MudBlazor-first vs token-CSS-first split is a *genuinely different philosophy* between two teams/eras of the same codebase, and Harmony offers a precedent for exactly one side of it (Section 10).

Why Harmony reads cleaner here: it made the extraction-bar decision once ("genuine reuse or inline it") and the empty `Harmony.Web.Shared` project is that decision made visible. EcoData made both decisions — build a shared layer *and* keep hand-rolling locally — and each app got a different mixture.

---

## 8. Reactivity & state

**Same philosophy:** page state lives in private fields of the rendering component; no stores, no Redux/Fluxor, no viewmodel layer (Harmony: zero; EcoData: one acknowledged survivor); a source-generator-driven attribute library provides loading/error state per async operation; app-level state services are few, small, and plain; both codebases keep the state library out of feature slices.

**Different:**

| Dimension | Harmony (Tempest.Blazor) | EcoData (legacy reactivity package, pre-migration) |
|---|---|---|
| Library usage | One library, one usage pattern, everywhere; `SPEC.md` authoritative; compile-time diagnostics TEM001–014 | One library, **disjoint subsets per app**: FaunaFinder uses only `Fetch<T>`; EcoPortal uses all four mechanisms incl. `[Signal]` — a second parallel reactivity system passing mutable state by reference as component parameters, inverting `EventCallback` flow; beta.6 restored but never adopted |
| Re-rendering | Every re-render funneled through `StatefulComponent`'s single marshalled `Rerender()`; manual `StateHasChanged` survives only at foreign edges (SignalR, `Virtualize`) | **134 manual `StateHasChanged` across 55 files — structural, not sloppy**: `Fetch<T>`'s constructor *requires* the re-render callback. EcoData's doc is explicit: any "no hand-written StateHasChanged" rule is incompatible with `Fetch<T>` as shaped |
| Mechanism count | Effectively one, plus two 20-line state services | FaunaFinder 9, EcoPortal 13 distinct mechanisms; FaunaFinder has **three localization channels firing on one event** |
| Loading/error triplet | Deleted by `CommandState` (`IsLoading`/`IsError`/`Result`) | Four loading conventions coexist, incl. hand-rolled `bool _isLoading` + try/finally alive in 7 EcoPortal components; `IFetch.IsError` never rendered inline |
| Latest-wins/cancellation | Built in: trailing `CancellationToken` = cancel-previous + stale-discard | Hand-rolled per page; undisposed per-load CTS found |
| Real-time push | SignalR outside the state library, hand-marshalled at the edge, then handed to the bus | **None exists**: SSE removed (#225), Service Bus terminates in server workers; `NotificationService.OnNotificationReceived` never fires yet is still subscribed by two components (#222/#224 track a future bridge) |
| Cross-component effects | Event bus with nested record contracts | `@ref` + `.Refresh()` choreography after every mutation; `Action?` events on singletons |
| Hygiene | Disposal handled by base class | Leaked `Fetch` disposables on 4 pages, 7 `async void` sites (including the public `Refresh()` API), one unmarshalled subscriber — but **zero unbalanced event subscriptions**, a genuine bright spot |

**Root cause:** the philosophies are near-identical — both codebases bet on "attribute + generated state twin per operation, fields in the component." The differences are (a) *library capability*: Tempest's base class absorbs marshalling, disposal, and cancellation that the legacy reactivity package pushes onto every call site, which is why EcoData's 134 `StateHasChanged` calls and disposal leaks are structural rather than careless; and (b) *enforcement*: nothing stopped EcoPortal from adopting `[Signal]` alongside `Fetch<T>`, or FaunaFinder from stacking three localization channels. Note the library question is entangled with real-time: Harmony's "manual StateHasChanged only at SignalR edges" idiom presumes SignalR exists; EcoData currently has no push at all (Section 10).

---

## 9. The meta-difference: how each codebase treats conventions

This is the difference that explains most of the others.

**Harmony: docs are source of truth.** `docs/ARCHITECTURE.md` and `CONVENTIONS.md` carry an explicit meta-rule: *"when the two disagree with existing code, these documents win — fix the code"* (Harmony preamble). Consequences visible throughout its survey:

- Drift is *named as drift*, with a "don't copy" label (`Guilds.Application → Music.Contracts`, Gaming's `Task<List<T>>` style, Identity's history table in `public`). One outlier slice, marked, not a second accepted style.
- Duplication is **deliberate and local**: the 8-line client method repeated per endpoint, byte-identical per-feature `ValidationFailed` copies, page-local formatting helpers — each duplicate is small, visible in one file, and exists to avoid a coupling abstraction. The docs say so ("duplication over coupling abstractions") and even inventory the costs accepted knowingly (55 projects, fat endpoints, consistency-by-copying "needs the docs to police it").
- Abstractions are added only when earned (`Internal` projects in 2 of 8 slices; a `Queries` class only when a filter actually repeats; "no private helper methods — inline it or promote it to a real seam").

**EcoData: docs are aspirational.** Its own survey states it as meta-observation #2: every dimension has documented rules the code contradicts — "No custom CSS files" vs 7,330 LOC of scoped CSS; the six-project module diagram no feature matches; no-`Include` vs 2; "never DB from endpoints" vs `GeoTestEndpoints`; migration docs citing components never used. Crucially, *nothing in EcoData states Harmony's meta-rule* — so when code and docs disagreed, the docs silently lost, every time. Consequences:

- Duplication is **unmanaged fork-drift**: three virtualized lists, two map stacks, two search bars, two layouts in copy-paste lineage, two themes, two `app.css`, duplicated `CommonErrors` — each pair started as a copy and diverged without anyone deciding it should.
- Abstractions were built speculatively as future unifications (Result monad, ValidationProblemDetail, messaging command layer, `LocalizedComponentBase` — "written to kill a pattern that then continued alongside it") and then bypassed, leaving *both* the abstraction and the N ad-hoc versions alive.
- New conventions accrete beside old ones instead of replacing them (four loading conventions; four write idioms; the 21-component legacy flat bucket beside feature folders).

The practical implication for Phase 2 is that adopting any individual Harmony convention without adopting the meta-rule reproduces EcoData's current state one refactor later: the survey shows EcoData has *already once* adopted most of Harmony's conventions in writing. The delta that matters is enforcement — docs that win, drift that gets fixed or explicitly labeled, and a habit of deleting the losing implementation when two exist.

(One caveat in fairness to EcoData: Harmony's docs are not spotless either — its own Tempest section is stale, with the survey directing readers to `SPEC.md` instead. The meta-rule is a discipline, not a guarantee.)

---

## 10. Open questions — where Harmony's convention may not fit

These are decisions, not recommendations. Each is a place where copying Harmony is either impossible, lossy, or contested by an EcoData convention that is itself healthy. Phase 2 needs an explicit call on each.

1. **Database-per-feature vs schema-per-feature (and snake_case vs PascalCase).** Harmony: one database, schema-per-feature, PascalCase, one shared `NpgsqlDataSource`. EcoData: five databases in `public`, snake_case — and the snake_case option block is EcoData's *strongest* convention, not drift. Trade-offs: Harmony's shape gives one connection pool and cheap cross-feature joins-by-hand in psql; EcoData's gives harder isolation and an easier future service split, at the cost of the Seeder being the only thing that can touch all five. Converging on either naming convention means either mass-renaming every table or writing down a permanent, principled divergence from Harmony.

2. **Migration application: Seeder worker vs per-feature startup.** Harmony migrates at startup under a documented, AppHost-enforced single-replica assumption. EcoData routes all migration (and cross-database seeding) through the 1,128-line idempotent Seeder that apps `.WaitFor()`. The Seeder is currently undocumented as the sanctioned cross-database writer; whichever way this goes, that role needs to be either documented or dissolved.

3. **Messaging and multiple hosts.** EcoData has Azure Service Bus events (#223), two web apps composing different feature subsets, `Sensors.Ingestion`, and homeless workers awaiting a worker host. Harmony is single-server, synchronous, and lists Commands/Events as "not yet introduced" — it simply has no convention to adopt here. Decision needed: keep `IMessageBus` events as a first-class EcoData convention (and write the rules Harmony never had: who may publish, where handlers live, what the ~1,400 LOC of dead command/handler/SSE machinery becomes), or shrink toward Harmony's sync `Application.Server`-only model and accept coupling across hosts.

4. **Tempest.Blazor vs the legacy reactivity package.** *(Since resolved: both Blazor clients migrated to Tempest.Blazor in #229.)* Same philosophy, different libraries. Tempest deletes the `StateHasChanged` callback, disposal burden, and cancellation boilerplate that account for a large share of EcoData §6's defect list — but it is beta, sourced from a sibling repo, targets net10.0, and its idioms assume SignalR-style push at the edges, which EcoData currently lacks (see #5). Staying on the legacy library means Harmony's "zero manual StateHasChanged" and disposal rules are *unadoptable as written* (EcoData's doc is explicit about the incompatibility); switching means migrating 138 razor files across two apps that today use disjoint subsets of the current library.

5. **Real-time push.** Harmony's UI conventions (stream clients, bus hand-offs, edge-marshalled SignalR) presuppose push. EcoData deliberately removed SSE (#225), has zero SignalR, and has a never-firing `NotificationService` event with live subscribers and tracking issues (#222/#224). Decision: introduce a push channel (which one, terminated where, bridged how from Service Bus) or codify "no push" and delete the vestigial subscriber surface. Adopting Harmony's UI conventions without deciding this imports idioms for a mechanism that doesn't exist.

6. **MudBlazor-first vs token-CSS-first (one app philosophy or two).** Harmony proves the MudBlazor-raw + one-global-css model works — for one app with one design language. EcoData has two apps with two: EcoPortal (Mud-first, feature-sliced pages) and FaunaFinder (semantic HTML + `fauna-tokens.css`, the solution's only token layer). Options each have real costs: converge both on Mud-first (lose the token layer, churn FaunaFinder), converge on tokens (contradict Harmony's precedent, churn EcoPortal), or bless two documented philosophies (then the shared component libraries must serve both, which is partly how NativeUi ended up 6/9 unused). Related sub-decisions regardless of direction: the fate of the 68 `.razor.css` files vs Harmony's zero-isolation rule, and the missing dark-mode toggle both apps' palettes assume.

7. **Localization/i18n.** EcoData needs it (Common.i18n, Wildlife's JSONB `LocaleValue` columns, FaunaFinder's localized UI); Harmony has nothing — zero precedent for locale-aware DTO projection, JSONB owned types (Harmony has "zero owned types, zero JSON columns"), or a localization re-render channel. The convention has to be authored, not adopted — and EcoData's current three-channels-one-event implementation shows what happens without one. Wildlife's JSONB/owned-type constructs should be evaluated as the i18n convention candidate, not dismissed as drift.

8. **Spatial/PostGIS.** EcoData runs a PostGIS image and has real mapping surfaces (two Leaflet stacks, Common.Maps); Harmony is non-spatial. Yet today's only spatial *database* artifacts are Wildlife's zero-column NetTopologySuite plumbing and a test endpoint. Decision: define where geometry actually lives (which feature, which columns, what the repository/DTO shape for spatial queries is) or strip the plumbing until it's earned. Harmony offers no guidance either way.

9. **Integration tests.** EcoData has them (Auth/Municipality/Organization/Sensors); Harmony has none — and Harmony's doc flags this as "a deliberate gap, not a convention to copy." So the tests must survive Harmonization, but they currently bypass the `Add<X>Database` registration conventions, re-register contexts three ways, and lose snake_case/NoTracking — meaning they don't exercise the very conventions being harmonized. Decision needed on the sanctioned test-registration path, a question Harmony cannot answer.

10. **`IDbContextFactory`-per-method vs injected DbContext.** EcoData's single most-followed convention (181 uses, zero exceptions) vs Harmony's primary-ctor injection. Not merely stylistic: it interacts with the pooling split (EcoData's two registration patterns exist partly to serve the factory), with Blazor lifetime concerns, and with any future move of workers out of hosts. Adopting Harmony's shape would overwrite EcoData's best-enforced habit; keeping the factory means documenting a deliberate, permanent divergence — and fixing the pooled-factory bridge either way.

11. **Genomics/Zig.** ~1,458 LOC hand-rolled Zig HTTP server, custom `AddZigApp` Aspire resource, run-mode only, no contracts/DB/client, loose files in the slnx. It sits outside every .NET convention in both documents. EcoData's own doc says it "needs a status decision": promote it to a real feature (contracts, client, manifest inclusion, conventions authored for a polyglot slice — no Harmony precedent) or formally mark it experimental and fence it off.

---

*End of Phase 1 analysis. Phase 2 (decisions and plan) should start from Section 10.*
