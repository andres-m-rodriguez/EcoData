# Harmonize Refactor — Phase 1 Findings

Survey of Harmony's conventions (`C:\Users\Overlord\OneDrive\Desktop\Projects\Harmony`) versus EcoData's current state, focused on the Fauna domain (`src/Features/Wildlife` + `src/Apps/FaunaFinder`). Phase 1 is analysis only — no implementation plan yet.

Harmony's authoritative docs: `docs/ARCHITECTURE.md` and `docs/CONVENTIONS.md` (rule: "when the docs disagree with code, fix the code"). Note the conventions doc has drifted on Tempest's API — trust the code and `C:\Users\Overlord\OneDrive\Desktop\Projects\Tempest\SPEC.md` for that.

---

## 1. The big picture: why Harmony reads cleaner

Harmony's simplicity comes from **absence, not abstraction**. The load-bearing decisions:

1. **No horizontal Application layer.** A "use case" is a minimal-API endpoint lambda. No MediatR, no handlers, no command/query classes, no application services. Logic moves to a class (`<Feature>.Internal`, behind a public interface) only when it becomes host-agnostic background work.
2. **The HTTP status code is the error code.** Errors are `OneOf` values, never exceptions; the shared error framework is 3 files; the UI maps status → copy at the point of display.
3. **No homemade component library.** MudBlazor used raw; the design system is one `MudTheme` class + one global CSS file. Zero wrapper components.
4. **One reactivity mechanism** (Tempest.Blazor) instead of many coexisting ones.
5. **Extreme consistency**: every repository, every client method, every list query, every endpoint follows the exact same shape. Reading one slice teaches you all eight.
6. **Duplication is preferred over shared abstractions** when the abstraction would couple features (e.g. `ValidationFailed` records are byte-identical copies per feature, on purpose).

---

## 2. Feature-slice architecture (Application layer conventions)

### Slice shape — projects added only when earned (6–8 per slice)

```
Harmony.<Feature>.Contracts            leaf; ZERO refs (some take only FluentValidation)
  Dtos/  Parameters/  Errors/  Validators/
Harmony.<Feature>.Database             EF Core packages only; no project refs
  <Ctx>.cs  <Ctx>Factory.cs  Models/  Migrations/
Harmony.<Feature>.DataAccess           → Contracts + Database; flat I<X>Repository + <X>Repository pairs
Harmony.<Feature>.Application          interfaces only (I<X>Client) → Contracts + Common.ProblemDetails + OneOf
Harmony.<Feature>.Application.Client   HTTP impls → Application + Contracts
Harmony.<Feature>.Application.Server   in-process cross-feature interface (I<X>Server)
Harmony.<Feature>.Internal             only Guilds & Music have it (bots, ingestion, pollers)
Harmony.<Feature>.Api                  endpoints + hubs + <X>Server impl + DependencyInjection
```

**Naming trap:** in Harmony, `Application` = "how apps talk to this feature" (client interface contract), NOT a business-logic layer.

### Key rules
- `Database`, `DataAccess`, `Internal` are feature-private; entities never leave the slice.
- Cross-feature calls: consumer references only the other feature's `Application.Server` + `Contracts`. Impl is `internal sealed` in the owning `Api` project, a thin adapter over its own repositories. Doc comments note "becomes gRPC when we split." No event bus, no messaging — synchronous, DI-injected, traceable.
- Registration: two extensions per feature (`Add<X>Feature()` / `Map<X>Endpoints()`), each layer with its own static `DependencyInjection` class. Host `Program.cs` is a flat greppable list — no assembly scanning, no module reflection.
- Endpoints: static extension classes, `Results<...>` typed unions + `TypedResults.*` always, deps method-injected per endpoint, `[AsParameters]` binding of Parameter records, no private helper methods (explicit rule — inline it or promote it to a real seam).
- Validation: FluentValidation validators in `Contracts/Validators/`, **never DI-registered** — both server endpoints and Blazor pages instantiate them inline (`new LoginDtoValidator().Validate(dto)`). Same class, both sides, zero infrastructure.
- Honest costs: 55 projects across 8 features; fat endpoint files (277-line ceiling); no tests in Harmony at all; sync cross-feature failure coupling. For EcoData, a slice could collapse to 3–4 projects (`Contracts`, `Database`+`DataAccess`, `Api`) and add `Application*`/`Internal` only when a second client / cross-feature caller / worker appears.
- Known Harmony drifts (don't copy): Guilds.Application references Music.Contracts; Identity's migrations-history table lands in `public` schema (missing `MigrationsHistoryTable(...)`).

## 3. Data access

- **Repositories: yes, flat, hand-written, one per resource.** `internal sealed`, primary ctor (DbContext + `TimeProvider`), only the interface public. No generic repository, no specification pattern, no unit of work, no base class.
- **Inline projection into Contracts DTOs in every `Select`** — no mappers, no shared projection expressions, no AutoMapper. Consequently zero `AsNoTracking()` and zero `Include()` in the whole solution.
- Writes prefer `ExecuteUpdateAsync`/`ExecuteDeleteAsync` (no load-mutate-save).
- **Inputs**: reads take a `<Resource>Parameter` record; writes take a `<Verb><Resource>Dto`. A Parameter never carries data to be written. Never `Get<X>By<Y>` naming — distinct methods with distinct Parameters.
- **Returns**: `Task<OneOf<TDto, NotFound>>` for fallible single reads; `IAsyncEnumerable<TDto>` (streamed, `[EnumeratorCancellation]`) for lists; `OneOf.Types.Success/NotFound` reused, feature errors in `Contracts/Errors/`.
- **Pagination**: keyset cursor over `Guid.CreateVersion7()` ids (generated inside repositories). Uniform 5-step list shape: base filter → optional filters via `if (param.X is T x)` → cursor `Where(x => x.Id.CompareTo(cursor) < 0)` → `OrderBy` + `Take(Math.Clamp(param.Take ?? DefaultTake, 1, 100))` → project → stream.
- Lambda params named after the entity (`guild =>`), never `x`.
- Composable query concession only when duplication appears: static `<Resource>Queries` class of `IQueryable<T>` extensions.
- Reference slices to clone: **Music** (richest), **Guilds** (smallest complete).

## 4. Database

- One Postgres, **schema-per-feature** (`friends`, `music`, …), per-schema `__EFMigrationsHistory`, one shared `NpgsqlDataSource` registered by host; feature `Api/DependencyInjection.cs` wires its own `AddDbContext` onto that pool. Microservice-shaped seams at monolith cost.
- DbContext per feature: primary ctor, expression-bodied `DbSet => Set<T>()`, `HasDefaultSchema("<feature>")` first line after `base.OnModelCreating`.
- **All mapping inline in `OnModelCreating`** — zero `IEntityTypeConfiguration`, zero data annotations, zero owned types/value converters/JSON columns/query filters/base entities. No enums in the database — enum-ish columns are strings with `HasMaxLength` "so the schema reads plainly."
- Entities: flat `sealed` POCOs; `required` on every mapped property **including nullable ones**; navigations always nullable; collections `= [];`; `DateTimeOffset` timestamps; `HasMaxLength` on every string; **a comment on every index naming the query it serves** (their most distinctive habit).
- Cross-feature FKs are soft (bare Guid + index, no constraint across schemas).
- Migrations named `<Verb><Noun>`, per-feature design-time factory pinning provider + history table, applied sequentially at startup (documented single-replica assumption).

## 5. HTTP clients & error handling

**The entire shared error framework is 3 files, zero deps** (`Harmony.Common.ProblemDetails`):
- `ProblemDetailsDocument` — record mirroring RFC 9457 + `errors` map + `AllMessages` flattener.
- `ProblemDetailsParser` — `ParseAsync`/`Parse`, return `null` instead of throwing. "Parsers only — no ensure/throw policy."
- `RequestFailed(int StatusCode, string? Message = null)` — the single transport-generic error.

**Server**: `TypedResults` unions; validation via inline `new XDtoValidator().ValidateAsync(dto)` → `TypedResults.ValidationProblem(validation.ToDictionary())`; business failures via `TypedResults.Problem("msg", statusCode: 403/409)`; repo `OneOf` → HTTP union with one inline `Match`. **No `AddProblemDetails`, no `UseExceptionHandler`, no `IExceptionHandler` anywhere.**

**Client**: typed `HttpClient` per feature (`<Noun>Client` impl of `I<Noun>Client`), no base class, no `SendAsync<T>` helper — each method is ~8 deliberately repeated lines:

```csharp
var response = await httpClient.GetAsync("music/tracks/{id}", ct);
if (!response.IsSuccessStatusCode)
    return new RequestFailed((int)response.StatusCode);
var track = await response.Content.ReadFromJsonAsync<TrackDetailsDto>(ct);
if (track is null)
    return new RequestFailed((int)response.StatusCode, "The server returned an empty track response.");
return track;
```

Plus 3 lines of `ProblemDetailsParser.ParseAsync` where per-field validation or detail text matters. **Clients never throw, never `EnsureSuccessStatusCode`**; `HttpRequestException` caught only where needed → `RequestFailed(0, msg)`. DI: `Add<X>Client(Action<HttpClient>, Action<IHttpClientBuilder>?)` two-delegate shape so each host injects its own auth handler. One `DelegatingHandler` (401 → login redirect, with auth-endpoint carve-out). No Polly, no resilience, no custom timeouts.

**UI**: `TryPickT0` happy path, `Match` the remainder, `switch` on status code for user-facing copy at the point of display. Unions grow per-method only when a caller actually branches (`OneOf<T, RequestFailed>` covers most; max 4 cases anywhere). Validation flows: shared validator client-side first → server authoritative → problem+json `errors` → `ValidationFailed.AllMessages` → flat list in a `MudAlert` (no per-field re-binding, no `EditForm`).

## 6. Blazor components

- **MudBlazor 9.6 raw — zero wrapper components.** No `HarmonyButton`/`HarmonyTable`; shared-UI project is intentionally empty. 36 `.razor` files total.
- Design system = `HarmonyTheme.cs` (~75 lines: palette, radius, typography) + one global `app.css` (BEM-ish `.harmony-*` for what Mud lacks). No CSS isolation, no code-behind, no SCSS/Tailwind.
- **Feature slices ship zero UI** — only typed clients + DTOs; the app project owns all rendering.
- **Dumb shared components, smart pages**: shared components 20–100 lines (parameters + one job); pages 400–600 lines owning fetching, SignalR, error copy, markup in one file. Extraction bar = genuine reuse across 2+ pages.
- Loading state is a repeated 3-branch idiom (error alert / null spinner / content), not a component. Empty states are literal sentences with a next action.
- Cross-tree communication via Tempest event bus + nested record contracts (`Bus.Publish(new ShareTrackSheet.Open(...))`); only 3 `EventCallback`s and 1 cascading parameter (auth) in the app.
- Dialogs: declarative `MudDialog @bind-Visible` + `DialogService.ShowMessageBoxAsync` for confirms; toasts via `ISnackbar`. Forms: raw `<form @onsubmit>` + Mud fields (no `EditForm`). Long lists: framework `<Virtualize ItemsProvider>`. JS interop: two tiny on-demand ES modules.

## 7. Reactivity: Tempest.Blazor

- Package: **`Tempest.Blazor` 1.0.0-beta.6** (+ transitive `Tempest.Abstract` carrying attributes, state types, event bus, and the Roslyn generator). Source: `C:\Users\Overlord\OneDrive\Desktop\Projects\Tempest` (`SPEC.md` is authoritative). Desktop uses `Tempest.WinUI`.
- Wiring: `builder.Services.AddTempest();` (registers scoped `IEventBus`) + `@inherits StatefulComponent` / `StatefulLayoutComponent` per component. Base class injects `Bus`, funnels ALL re-renders through one marshalled `InvokeAsync(StateHasChanged)`, and disposes bus subscriptions.
- Attributes → generated twins:
  - `[Reactive] private T _foo;` → `FooState : ReactiveState<T>` (`Value`, `SetSilently`, `IsDirty`, `Reset`)
  - `[Command] private Task<T> Name(CancellationToken ct)` → `NameState : CommandState<T>` (`IsLoading`, `IsError`/`Error`, `Execute`/`TryExecute`, `Result`; trailing `CancellationToken` = latest-wins cancellation + stale-result discard)
  - `[Event] private void OnX(NestedRecord e)` → bus subscription; nested public records are the component's public API
  - `[OnChanged]` hook per reactive field; `[RunOnLoad]` fire-and-forget initial load into `State.Error`
- Practice: no stores/viewmodels for page state — private fields in the rendering component; only two app-level state services (plain scoped classes taking `IEventBus`). Markup binds `SearchState.Value`; buttons bind `XState.IsLoading`. Deletes the `_isLoading/_error/_result` triplet per operation and all hand-written `StateHasChanged` except SignalR/Virtualize edges.
- SignalR stays outside Tempest: plain singleton `<X>StreamClient` exposing C# events; hub callbacks hand-marshal with `InvokeAsync` then hand off to the bus.
- Compile-time diagnostics TEM001–TEM014 guard misuse.
- Minimum port: PackageReference + `AddTempest()` + `@using Tempest` in `_Imports` + `@inherits StatefulComponent`. Targets net10.0.

---

## 8. EcoData Fauna — the "before" picture

Fauna = `src/Features/Wildlife` (6 projects, ~3,800 LOC) + `src/Apps/FaunaFinder` (Blazor WASM, ~6,400 LOC). Read-only feature, zero write endpoints, zero tests.

### Already Harmony-shaped (keep)
- Inline DTO projection in repositories; entities never leave DataAccess.
- Per-feature DbContext, snake_case, Aspire Npgsql.
- Feature-slice project layout broadly mirrors Harmony's naming.

### Complexity hot spots (ranked)
1. **Dead abstraction layers**: `Wildlife.Application.Server` is a 16-line no-op that drags Identity + Azure Service Bus into FaunaFinder.Server; `EcoData.Common.Results` is ~380 LOC of Result monad used by 4 files solution-wide (none in Fauna); FluentValidation/OneOf/OneOf.SourceGenerator/Problems.Contracts referenced but unused; `SpeciesDtoForCreate/ForUpdate` unreachable.
2. **Two forked virtualized grids**: `NuiVirtualizedGrid` (NativeUi) vs `EcoDataVirtualizedGrid`/`List` (Pagination.Blazor) — ~600 LOC near line-for-line duplicated cursor-cache logic, both with `async void Refresh()` and hand-rolled `_generation` cancellation.
3. **Errors are erased, not handled**: every non-2xx → `null`/`[]`/`0`; UI can't distinguish 404 from 500; failed count renders as "0 species"; one streaming call uses null-forgiving `!` with no try/catch (raw throws inside `Virtualize`). Two complete error frameworks exist in the solution and Fauna uses neither. Traced round trip: 13 hops, 0 error types, 1 sentinel.
4. **Five coexisting reactivity mechanisms**: BlazingSingularity `Fetch<T>` (5 on one page, manual dispose/recreate), grid-internal cache, two `Action?` event singletons, `LanguageChanged` event subscribed in 6+ components, parallel `CascadingValue<LocaleContext>` — plus shadow-field dirty-checking. `LocalizedComponentBase` (written to fix this) has zero subclasses. Window resize refetches the list from page 1.
5. **Interface/impl pairs with no polymorphism**: 5 HTTP-client pairs (two are 14-line single-method files), 3 repo pairs; nothing mocked, no tests.
6. **Mega-pages doubling as list AND detail** (two `@page` directives, `Id is not null` branching, re-entrancy guards).
7. **Per-call DbContext factory + N-query aggregates** (`GetStatsAsync` = 6 sequential queries).
8. **Known-broken cursor pagination** (issue #188): cursor is Id-based but default sort is ScientificName; 2 of 4 sorts silently return page 1 forever.
9. **Localization split three ways** (460-LOC strings class, cascading LocaleContext, ad-hoc `FirstOrDefault(n => n.Code == "en")` re-implemented per component).
10. **CSS contradicts own docs**: migration plan says "MudBlazor utilities only," app ships ~1,900 LOC scoped `.razor.css` across 20 files.
11. Oddities: `_Components.cs` namespace marker class, 12 per-folder `@using` lines for NativeUi, hand-rolled query-string builder duplicating an existing helper, unawaited fetch fired inside another fetch's lambda.

---

## 9. The delta in one table

| Concern | Harmony | EcoData/Fauna today |
|---|---|---|
| Use cases | Endpoint lambdas; `Internal` services when earned | Endpoints → repos directly (same!), but dead `Application.Server` stub in the graph |
| Errors | 3-file ProblemDetails lib + `RequestFailed` + per-method `OneOf`; status code = error code | Two unused frameworks; actual behavior: errors erased to `null`/`[]`/`0` |
| Components | MudBlazor raw; theme + 1 CSS file; zero wrappers | Homemade NativeUi (32 files) + forked grid in Pagination.Blazor + Common.Maps; scoped CSS everywhere |
| Reactivity | Tempest.Blazor: `[Reactive]`/`[Command]`/`[Event]`, one re-render funnel | 5 mechanisms: BlazingSingularity Fetch, grid cache, 2 event singletons, cascading locale, shadow fields |
| Validation | FluentValidation in Contracts, inline both sides | Package referenced, zero validators |
| Pagination | Uniform keyset over GUIDv7, clamped Take | Broken Id-cursor vs name-sort; hand-rolled query strings |
| DB config | Inline OnModelCreating, no annotations/owned types/enums | Nested EntityConfiguration classes, JSONB owned collections (`LocaleValue`), global NoTracking + pool + factory |
| Consistency | Every slice identical; docs are source of truth | Per-page divergence; docs contradicted by code |

## 10. Open questions for Phase 2 (not decisions yet)

- Slice sizing: full 7-project Harmony shape vs collapsed 3–4 projects per EcoData feature.
- Tempest targets net10 — EcoData is net10, OK; pin to beta.6 or update Tempest first?
- Localization/JSONB `LocaleValue`: Harmony has no i18n story — this is EcoData-specific and needs its own convention (single locale channel).
- MudBlazor version bump (EcoData 9.1.0 → Harmony 9.6.0) and NativeUi retirement strategy (which Nui components have no Mud equivalent — e.g. NuiMap stays).
- Whether to keep `IAsyncEnumerable` streaming lists (Harmony) vs buffered — interacts with the virtualized-grid replacement.
- Test strategy: Harmony has no tests; EcoData has integration tests for other features — decide the bar for refactored Wildlife.
