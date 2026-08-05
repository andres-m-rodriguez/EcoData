# Harmony — Conventions Document

How Harmony (`C:\Users\Overlord\OneDrive\Desktop\Projects\Harmony`) is built, dimension by dimension. Compiled from a seven-agent survey (2026-08-05) for the EcoData "Harmonize" refactor. Companion documents: `ecodata-conventions.md` (our codebase) and `comparison.md` (similarities/differences).

Harmony keeps its rules in `docs/ARCHITECTURE.md` and `docs/CONVENTIONS.md`, and treats them as source of truth: *"when the two disagree with existing code, these documents win — fix the code."* Exception: the Tempest section of CONVENTIONS.md is stale (describes the pre-beta.2 API); for Tempest, trust the code and `C:\Users\Overlord\OneDrive\Desktop\Projects\Tempest\SPEC.md`.

## 0. Solution shape

```
src/
├── Apps/       Harmony.Desktop (WinUI 3) · Harmony.Web.Client (Blazor WASM) ·
│               Harmony.Web.Server · Harmony.Web.Shared (intentionally empty)
├── Common/     Harmony.Common.Images · Harmony.Common.ProblemDetails
├── Features/   Friends · Gaming · Guilds · Identity · Messaging · Music ·
│               Notifications · Recommendations   (8 slices, 6–8 projects each, 55 total)
└── Harmony.AppHost   (Aspire)
```

Feature-based modular monolith. Vertical cut first; layering is a detail *inside* each slice. Deleting a feature = deleting a directory + 3 lines in `Program.cs`.

## 1. Feature-slice anatomy

```
Harmony.<Feature>.Contracts            leaf: ZERO project refs (some take only FluentValidation)
  Dtos/  Parameters/  Errors/  Validators/  (+ invariant helpers e.g. MusicNormalization)
Harmony.<Feature>.Database             EF Core packages only, no project refs
  <Ctx>.cs  <Ctx>Factory.cs  Models/  Migrations/
Harmony.<Feature>.DataAccess           → Contracts + Database
  I<X>Repository.cs + <X>Repository.cs pairs, flat, + DependencyInjection.cs
Harmony.<Feature>.Application          → Contracts + Common.ProblemDetails + OneOf
  interfaces only: I<X>Client, I<X>StreamClient
Harmony.<Feature>.Application.Client   → Application + Contracts
  <X>Client.cs (HttpClient impls), <X>StreamClient.cs (SignalR), DependencyInjection.cs
Harmony.<Feature>.Application.Server   → Application + Contracts
  I<X>Server — the ONLY surface other features may call
Harmony.<Feature>.Internal             only where earned (Guilds: Discord bot; Music: ingestion)
Harmony.<Feature>.Api                  → own Contracts/DataAccess/Internal/Application.Server
                                         + other features' Application.Server + Contracts
  Endpoints/  Hubs/  <X>Server.cs impl  DependencyInjection.cs
```

Projects are added only when earned: Gaming and Recommendations have 6, most have 7, Guilds/Music have 8 (they add `Internal`).

**Naming trap:** `Application` here means "the client-facing interface contract" — how apps talk to the feature — NOT a business-logic layer. There is no business-logic layer.

### Dependency rules (compiler-enforced by project refs)
- `Database`, `DataAccess`, `Internal` are feature-private; nothing outside the slice references them.
- Database entities never leave the slice — mapped to Contracts DTOs inside DataAccess.
- `Api` is a leaf: only the host `Program.cs` references it.
- Cross-feature: consumer references only the other feature's `Application.Server` + `Contracts`. The impl is `internal sealed` in the owning `Api`, a thin (often ~11-line) adapter over its own repositories, with "becomes gRPC when we split" doc comments.
- Known drift (don't copy): `Guilds.Application` references `Music.Contracts`.

### Use cases = endpoint lambdas
No MediatR, no mediator, no handlers, no command/query classes, no application services, and no domain model (no aggregates, value objects, domain events, specifications). A use case is written inline in `MapPost(...)` in `<Feature>.Api/Endpoints/<X>Endpoints.cs`: auth resolution, cross-feature calls, business branching, SignalR push — all visible in one lambda with dependencies method-injected in the parameter list. ~65 lines for "send a friend request"; 277-line endpoint file is the ceiling.

Logic moves out only when it becomes host-agnostic/background work → `internal sealed` service behind a public interface in `<Feature>.Internal` ("moving a BackgroundService from the web host to a worker is a Program.cs change, not a feature change").

Style rules: `Results<...>` typed unions + `TypedResults.*` always (never untyped `Results.*`); `[AsParameters] <X>Parameter param` binding; braceless guard clauses; **no private helper methods** (explicit rule — inline it or promote it to a real seam); endpoint aggregators nest (`MapMusicEndpoints` → `MapListenEndpoints` + `MapStatsEndpoints` + …).

### Registration: the module pattern, no magic
Each layer has its own static `DependencyInjection` class; `Add<X>Feature()` in the `Api` project composes them and registers the feature's DbContext onto the shared `NpgsqlDataSource`. Host `Program.cs` is a flat greppable list: 8 `Add<X>Feature()` + 13 `Map<X>()` calls. No assembly scanning, no `IModule` reflection. Background work registers via separate opt-in extensions so a future worker host can adopt it without touching feature code.

### Cross-feature communication
Direct, synchronous, DI-injected `I<X>Server` interfaces — no event bus, no messaging (docs list Commands/Events as "not yet introduced"). Producers call consumers inline after their own mutation succeeds. Live push to browsers is a separate parallel mechanism (SignalR hub groups). Cost accepted knowingly: a Notifications failure fails the Friends request; the `Application.Server` seam is the future async swap point.

## 2. Data access

- **Flat hand-written repositories, one per resource.** `internal sealed`, primary constructor (DbContext + `TimeProvider`), only the interface public. No generic `IRepository<T>`, no specification pattern, no unit of work, no base class, no `Repositories/` subfolder.
- **Inline projection into Contracts DTOs in every `Select`.** No AutoMapper, no `ToDto()` extensions, no shared projection expressions. Consequently **zero `AsNoTracking()` and zero `Include()` in the entire solution** — projections make both moot.
- Writes prefer `ExecuteUpdateAsync` / `ExecuteDeleteAsync` (24 usages) over load-mutate-save.
- **Inputs:** reads take a `<Resource>Parameter` record; writes take a `<Verb><Resource>Dto`. "A Parameter never carries data to be written." Never `Get<X>By<Y>` naming — distinct methods with distinct Parameter types (`GetTrack(TrackParameter)` vs `GetExternalTrack(ExternalTrackParameter)`). No variant-baked methods (no `GetTopArtists`) — one method per projection, Parameter carries filter + sort enum + size.
- **Returns:** `Task<OneOf<TDto, NotFound>>` for fallible reads; `IAsyncEnumerable<TDto>` for lists (streamed via `AsAsyncEnumerable()` + `[EnumeratorCancellation]`); `OneOf.Types.Success`/`NotFound` reused rather than reinvented; feature-specific errors are plain records in `Contracts/Errors/`. Never exceptions, never a home-grown `Result<T>`.
- **Pagination: keyset cursor over GUIDv7, uniform 5-step shape** in every list read across all 8 slices:
  1. `var query = context.X.Where(...)` base filter
  2. optional filters via `if (param.Foo is T foo)`
  3. `if (param.Cursor is Guid cursor) query = query.Where(x => x.Id.CompareTo(cursor) < 0)`
  4. `.OrderByDescending(x => x.Id).Take(Math.Clamp(param.Take ?? DefaultTake, 1, 100))` — `private const int DefaultTake = 50;` atop nearly every repository
  5. `.Select(x => new XDto(...))` → stream
  Ids are `Guid.CreateVersion7()` generated **inside repositories**, so id order = time order and the cursor is "the last id you saw." Compound keysets where needed (Listen: `(StartedAt, Id)`, rationale in a comment).
- Lambda parameters named after the entity (`guild => guild.Id`), never `x`.
- The only composition seam: a static `<Resource>Queries` class of `IQueryable<T>` extension methods, added only when a domain filter actually repeats.
- Normalization of match keys happens inside the repository (data invariant, not business logic).
- Known drift (the outlier): Gaming (SQLite/desktop) returns `Task<List<T>>`, uses short lambda names, has private static helpers.

**Reference slices to clone: `Music` (richest), `Guilds` (smallest complete).**

## 3. Database

- **One Postgres database, schema-per-feature** (`friends`, `guilds`, `identity`, `messaging`, `music`, `notifications`, `recommendations`), each with its own `__EFMigrationsHistory` inside its schema. Gaming is device-local SQLite on desktop. Aspire AppHost provisions Azure Flexible Server (container locally).
- **One shared `NpgsqlDataSource`** registered by the host (`builder.AddAzureNpgsqlDataSource("harmony")`); each feature's `Api/DependencyInjection.cs` calls `AddDbContext` onto that pool with `MigrationsHistoryTable("__EFMigrationsHistory", "<schema>")`. No connection strings in appsettings (logging config only).
- **DbContext per feature** in `<Feature>.Database`: primary constructor, expression-bodied `DbSet => Set<T>()` (never auto-properties), `HasDefaultSchema("<feature>")` as the first line after `base.OnModelCreating`. Identity is the only non-plain base (`IdentityDbContext<HarmonyUser, IdentityRole<Guid>, Guid>`).
- **Configuration: 100% inline `OnModelCreating`,** one lambda block per entity. Zero `IEntityTypeConfiguration`, zero data annotations, zero owned types, zero value converters, zero JSON columns, zero query filters, zero base entity / `IAuditable`, zero concurrency tokens. **No enums in any Database project** — enum-ish columns are strings with explicit lengths "so the schema reads plainly" in psql.
- **Entities: flat `sealed` POCOs.** Every mapped scalar/FK is `required` **even when nullable** ("writers state intent" — a new column becomes a compile error at every write site). Navigations always nullable even when the FK is required ("EF may simply not have loaded it yet"). Collections `= [];`. `DateTimeOffset` for all timestamps. Property comments earn surrounding blank lines; otherwise properties pack tight, one blank line between scalars and navigations.
- Sub-conventions in config blocks: lambda named after the entity; order Property → HasKey/ToTable → HasIndex → HasOne; `HasMaxLength` on **every** string; **a comment on every index naming the query it serves** (the most distinctive habit in the layer); `ToTable` only where pluralization needs a nudge.
- Cross-feature references are **soft**: bare `Guid` columns with an index, no FK across schema boundaries.
- **Migrations:** in `Migrations/` per Database project, named `<Verb><Noun>` (`AddRefreshTokens`, feature-suffixed initials like `InitialGuilds`). Per-feature design-time factory pins provider + history table so `dotnet ef` needs no host/connection string. Applied sequentially at startup in every environment (documented single-replica assumption, enforced in AppHost `MinReplicas=MaxReplicas=1`). Desktop/SQLite migrates lazily via `Lazy<Task>`.
- Package hygiene: each Database csproj restates identical pinned PackageReferences (no central package management) — deliberate self-containment.
- Known drift (don't copy): Identity omits `MigrationsHistoryTable`, so its history table lands in `public`.
- Tables/columns are PascalCase EF defaults — **no snake_case naming convention**.

## 4. HTTP clients & error handling

### The shared framework: 3 files, zero dependencies (`Harmony.Common.ProblemDetails`)
- `ProblemDetailsDocument` — record mirroring RFC 9457 + ASP.NET `errors` extension + `AllMessages` flattener.
- `ProblemDetailsParser` — `ParseAsync(HttpResponseMessage)` / `Parse(string)`; both return `null` rather than throw (null on non-problem+json content type, swallow `JsonException`). Documented rule: *"Parsers only — no ensure/throw policy."*
- `RequestFailed(int StatusCode, string? Message = null)` — the single transport-generic error. Status `0` = never reached the server.

No `Result<T>`, no error-code enums, no base `Error` class, no exception hierarchy, **no exception middleware at all** (no `AddProblemDetails`, `UseExceptionHandler`, `IExceptionHandler` anywhere). Unhandled exceptions get the ASP.NET default. **The HTTP status code is the error code.**

### Server side
- `Results<...>` unions + `TypedResults`: validation → `TypedResults.ValidationProblem(validation.ToDictionary())`; business failures → `TypedResults.Problem("message", statusCode: 403/409)` (12 sites); `TypedResults.NotFound()` / `.Unauthorized()`.
- Repositories return `OneOf`; the endpoint `Match`es that union onto the HTTP union — the only translation layer, written inline.

### Client side
- Typed `HttpClient` per feature: `I<Noun>Client` in `Application`, `internal sealed <Noun>Client(HttpClient httpClient)` in `Application.Client`; SignalR counterparts `<Noun>StreamClient` + `<Noun>StreamOptions`.
- DI: uniform two-delegate shape `Add<X>Client(Action<HttpClient> configureClient, Action<IHttpClientBuilder>? configureBuilder = null)` — lets Blazor WASM inject cookie auth + `UnauthorizedRedirectHandler` while Desktop injects service discovery + `BearerTokenHandler`, from the same client library.
- **No base class, no `SendAsync<T>` helper.** Every method is ~8 deliberately repeated lines: send → `if (!IsSuccessStatusCode) return new RequestFailed(...)` → deserialize → null-check → return. Plus 3 lines of `ProblemDetailsParser.ParseAsync` where per-field errors or detail text matter.
- **Clients never throw, never `EnsureSuccessStatusCode`.** `HttpRequestException` caught only where explicitly needed → `RequestFailed(0, msg)`.
- Error vocabulary: `OneOf<T, RequestFailed>` covers most methods; unions grow per-method only when a caller actually branches (max anywhere: `OneOf<Success, InvalidCredentials, ValidationFailed, RequestFailed>`). Empty markers are `readonly record struct`; `ValidationFailed` is **deliberately duplicated per feature** (byte-identical) to keep slices independent.
- The only client middleware: one `DelegatingHandler` redirecting 401 → login (with an auth-endpoint carve-out so contract-level 401s reach callers).
- **No Polly, no resilience handlers, no custom timeouts** — defaults accepted. Only SignalR's `.WithAutomaticReconnect()`.

### UI side
- `TryPickT0` for the happy path, `Match` on the remainder, `switch` on status code for user-facing copy **at the point of display** (no central error-message registry): `403 => "You can only recommend songs to your friends."`.
- Success = `ISnackbar` toast; failure = inline `MudAlert`. Transport exceptions surface via Tempest's `XState.IsError` → generic "please try again" banner.

### Validation
FluentValidation validators in `Contracts/Validators/`, `public sealed class XDtoValidator : AbstractValidator<XDto>` — only where real user input exists (5 of 8 features). **Never registered in DI; both sides instantiate inline**: server endpoint `await new XDtoValidator().ValidateAsync(dto)`, Blazor page `new XDtoValidator().Validate(dto)` for instant client feedback. Server errors flow back via problem+json `errors` map → `ValidationFailed.AllMessages` → flat list in one `MudAlert` (no per-field re-binding, no `EditForm`/`DataAnnotationsValidator` — raw `<form @onsubmit>` + `MudTextField Immediate="true"`). Business-rule checks needing data are inline endpoint checks or repository `OneOf` arms, not validators.

## 5. Blazor components

- **MudBlazor 9.6.0 used raw — zero wrapper components.** No `HarmonyButton`/`HarmonyCard`/`HarmonyTable`. `Harmony.Web.Shared` is intentionally empty. Whole web UI = 36 `.razor` files: `Pages/` (23), `Layout/` (11 + layout), `Auth/`, `Navigation/`, `Themes/`.
- **Feature slices contain zero UI** — they ship typed clients + DTOs; the app project owns all rendering. If the UI framework changed, no slice would notice.
- **Design system = `HarmonyTheme.cs` (~75 lines)**: one static `MudTheme` with palette, `DefaultBorderRadius`, full typography scale. Plus **one global `app.css`** (~2,200 lines, ~219 BEM-ish `.harmony-*` classes: block/element/`--modifier`) for what Mud doesn't ship (shell frame, chat bubbles, bottom bar). Spacing/layout via MudBlazor's own utility classes (`mt-3`, `flex-grow-1`). **No CSS isolation, no SCSS, no Tailwind, no build step, zero `.razor.cs`, zero `.razor.css`.**
- **Dumb shared components, smart pages.** Shared components 20–100 lines: parameters + one job, sensible defaults, contract stated in a comment. Pages 400–600 lines, owning data loading, SignalR subscriptions, disposal, error copy, and markup in one file. Extraction bar = genuine reuse across 2+ pages, never decomposition-for-its-own-sake. Median non-layout component ~130 lines; no "molecule" tier. Formatting helpers are `private static` in the page and cheerfully duplicated.
- **Communication:** Tempest event bus with nested public record contracts (`Bus.Publish(new ShareTrackSheet.Open(trackId, title, artist))`) instead of callback chains. Only 3 `EventCallback`s and exactly 1 `CascadingParameter` (framework auth state) in the whole app. RenderFragment: 2 uses.
- Two layouts (`MainLayout` default, 10-line `AuthLayout` per-page). Attribute routing with typed constraints; multi-route pages stack `@page` directives; `[Authorize]` attribute on 20/23 pages.
- **Loading state is a repeated idiom, not a component:** `_loadError` alert → null-sentinel spinner → content. Empty states are literal sentences with a next action ("No friends yet. Find people to add.").
- Dialogs: declarative `<MudDialog @bind-Visible>` with a `static readonly DialogOptions` field, `DialogService.ShowMessageBoxAsync` for confirms, `ISnackbar` for toasts. Long lists: framework `<Virtualize ItemsProvider>` with `ItemSize` — no homemade paging component. JS interop: two tiny on-demand ES modules (37 + 36 lines).
- Raw HTML only where articulable in one sentence (one native `<button>` for `@onclick:stopPropagation` inside an anchor).
- Responsiveness: CSS media queries + `MudHidden` when branches need different *components*, not different CSS.

## 6. Reactivity: Tempest.Blazor

- **Package: `Tempest.Blazor` 1.0.0-beta.6** (transitive `Tempest.Abstract` carries attributes, state types, event bus, and the Roslyn generator). `Tempest.WinUI` 1.0.0-beta.7 on desktop. **No other state library in the solution** (no Fluxor/ReactiveUI/R3/CommunityToolkit). Source: `C:\Users\Overlord\OneDrive\Desktop\Projects\Tempest`; `SPEC.md` there is authoritative. Targets net10.0.
- **Wiring:** `builder.Services.AddTempest();` (registers `IEventBus → EventBus`, scoped — that is the entire registration surface) + `@using Tempest` + `@inherits StatefulComponent` (layouts: `StatefulLayoutComponent`).
- `StatefulComponent` (~65 lines): injects `Bus`, calls generated `RegisterTempestHandlers` in `OnInitialized`, funnels **every** re-render through one marshalled `InvokeAsync(StateHasChanged)` (`Rerender()`), routes `[OnChanged]` hook throws to Blazor error boundaries, and disposes all bus subscriptions. Override pattern: unhook your own listeners, then `base.Dispose()`.
- **Attributes → generated partial-class twins:**
  - `[Reactive] private T _foo;` → `FooState : ReactiveState<T>`: `Value` (change-check → assign → re-render → hook), `SetSilently(v)` (no hook — for programmatic resets), `Initial`, `IsDirty`, `Reset()`.
  - `[Command] private Task<T> Name(CancellationToken ct)` → `NameState : CommandState<T>`: `IsLoading`, `IsError`/`Error`, `ClearError()`, `Execute()`/`TryExecute()`, `Result`/`HasResult`. Trailing `CancellationToken` buys latest-wins: cancel previous CTS, version bump, stale-result discard, re-render on both loading edges. Omit the token deliberately when latest-wins is wrong (comment why).
  - `[Event] private void OnX(SomeNestedRecord e)` → bus subscription. Event records are **nested public records on the component that owns the reaction**, doubling as its public API (`MainLayout.NotificationsSeen`, `BottomBar.Hidden`). Empty handlers are idiomatic — the subscription itself is the re-render.
  - `[OnChanged]` hook per reactive field (`OnSearchChanged(T value)`); `[RunOnLoad]` on a command = fire-and-forget initial load, failure lands in `State.Error`.
- **Practice:** no stores/viewmodels for page state — private fields in the rendering component. Exactly two app-level state services (`CurrentUserState` 20 lines, `PageNavigationCatalog`), plain scoped classes taking `IEventBus`. Markup binds the twin (`@bind-Value="SearchState.Value"`, `Disabled="XState.IsLoading"`, `@foreach (var x in LoadState.Result ?? [])`); raw field read for derived values. `[Command]` methods are private parameterless; per-invocation args staged into fields first. This deletes the `_isLoading/_error/_result` triplet per operation and nearly all hand-written `StateHasChanged`.
- **SignalR stays outside Tempest:** plain singleton `<X>StreamClient` exposing C# events over a `HubConnection` with `.WithAutomaticReconnect()`; pages subscribe in `OnInitialized`, connect best-effort in try/catch, hand-marshal callbacks with `InvokeAsync(... StateHasChanged())`, then hand off cross-component effects to the bus (`Bus.Publish<MainLayout.NotificationsSeen>()`). Manual `StateHasChanged` survives only at these foreign edges (SignalR, `Virtualize` refresh).
- Compile-time diagnostics TEM001–TEM014 catch misspelled hooks, parameterized commands, name collisions, missing `@inherits`.
- Canonical example (debounced search, `MainLayout`): Mud debounce 300ms → `SearchState.Value` → `[OnChanged]` → `SearchCatalogState.TryExecute()` → cancel-previous + spinner + commit + re-render. Zero hand-written `StateHasChanged`, zero manual loading/error fields.

## 7. Meta-conventions (the glue)

- **Docs are source of truth**; drift is visible as drift (Gaming) rather than an accepted second style.
- **Duplication over coupling abstractions**: repeated 8-line client methods, per-feature `ValidationFailed` copies, duplicated page-local formatting helpers — local duplication any reader can see beats distributed coupling.
- **No private helper methods** — inline it, or promote it to a real seam (an `Internal` service, a `Queries` extension class).
- `internal sealed` + primary constructors everywhere; only interfaces public.
- `TimeProvider` injected, never `DateTime.UtcNow` in repos.
- XML doc comments explain semantics, never restate names; comments state constraints/rationale ("The same-source idempotency guard: re-polling can never duplicate a listen").
- **No tests anywhere** (a deliberate gap, not a convention to copy).
- Costs accepted knowingly: 55 projects, fat endpoint files, sync cross-feature coupling, consistency-by-copying (needs the docs to police it).
