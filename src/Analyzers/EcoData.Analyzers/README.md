# EcoData.Analyzers

Code style conventions for the EcoData solution. Rules are enforced as Roslyn analyzers where possible; the rest are written conventions checked in review.

## Layout and wiring

- Analyzer classes live in `Rules/`, one file per rule.
- Code fixes live in `src/Analyzers/EcoData.Analyzers.CodeFixes` (a separate assembly; the compiler cannot load Workspaces-dependent code).
- Tests live in `tests/EcoData.Analyzers.Tests`.
- The root `Directory.Build.targets` references both analyzer projects with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"` from every project, so the rules run on every build. A project opts out only with `UseEcoDataAnalyzers=false`. Builds must stay at zero warnings.
- The root `.editorconfig` turns off IDE rules that contradict an ECO rule. IDE0200 (prefer method group) is off because ECO003 wants the lambda.

## Naming

- Methods never take the `Async` suffix, including task-returning ones.
- Interface methods take an explicit `CancellationToken cancellationToken` parameter without a default value.
- Extension members use C# 14 `extension` blocks, never `this`-parameter methods.

## Comments

- XML doc comments describe what a member is, never why the system has it. Shared libraries stay generic: no references to specific consumers or architecture rationale.
- Non-doc comments state only constraints the code cannot express itself.

## ECO001: Single-statement if should not use braces

An `if` or `else` body consisting of a single simple statement (expression, return, throw, break, continue, yield) is written without braces.

```csharp
// Wrong
if (problem is not null)
{
    return problem;
}

// Right
if (problem is not null)
    return problem;
```

Not flagged: multi-statement bodies, declarations and nested ifs (illegal or dangling-else-unsafe without braces), other constructs such as loops, and braces carrying comments or preprocessor directives.

## ECO002: Method call result should not be passed inline as an argument

The result of a method call is assigned to a local variable before being passed to another call. This includes awaited calls.

```csharp
// Wrong
context.ReportDiagnostic(Diagnostic.Create(Rule, location, "if"));

// Right
var diagnostic = Diagnostic.Create(Rule, location, "if");
context.ReportDiagnostic(diagnostic);
```

Not flagged (hoisting must be provably safe): `nameof(...)`, ref and out arguments, constructor arguments, calls where any observable work (a call, object creation, assignment, increment, or await) completes earlier in the same statement, and any context where a hoisted local would run at a different time, frequency, or scope: lambda and expression-bodied members, conditional access chains, ternaries and short-circuit operators, switch and query expressions, case when clauses, catch filters, object, collection, array, and with initializers, and while, do, and for loop headers (foreach sources evaluate once and are still flagged).

## ECO003: Method group should not be passed as an argument

A callback argument is written as a lambda, so the call it makes and the values it forwards are visible where it is passed.

```csharp
// Wrong
return result.MapT1(RequestFailed.From);

// Right
return result.MapT1(problem => RequestFailed.From(problem));
```

Flagged: any expression that binds to a method and converts to a delegate while sitting in an argument list, including constructor arguments. Not flagged: lambdas, delegate-typed locals, `nameof`, invocation results, and method groups in assignments or event subscriptions. The fix names the lambda parameters after the target method's own parameters, with a numeric suffix when a name is already in use.

## ECO004: Private method should be static

A private method is static. Everything it works on arrives through its parameters, so the signature states its intent and the instance state stays with the caller. A private method that needs instance state is either given that state as a parameter or inlined into the caller.

```csharp
// Wrong
private bool IsOverdue(Reservation reservation) => reservation.DueOn < _clock.Today;

// Right
private static bool IsOverdue(Reservation reservation, DateOnly today) => reservation.DueOn < today;
```

Flagged: every ordinary method whose declared accessibility is private, whether the modifier is written or implied. Not flagged: constructors, accessors, operators, local functions, explicit interface implementations, partial methods, and `private protected` members. The fix adds `static` only when the body touches no instance member of the containing type (no `this`, `base`, or implicit member access); a body that does needs a parameter the fix cannot invent.

## ECO005: Private method with one call site should be inlined

A private method that is called from exactly one place is inlined into that caller, so the logic is read where it runs and no one-off helper hides it. References are counted across every declaration of the containing type, nested types included, and the single reference must be a direct invocation.

```csharp
// Wrong
void M(int total)
{
    Log(total + 1);
}

private void Log(int number)
{
    var text = number.ToString();
    Console.WriteLine(text);
}

// Right
void M(int total)
{
    var number = total + 1;
    var text = number.ToString();
    Console.WriteLine(text);
}
```

Not flagged: methods with zero references or two or more (a recursive call counts), any reference that is not an invocation (method group, `nameof`, delegate creation, attribute argument), methods carrying an attribute, partial methods, explicit interface implementations, local functions, generic methods, methods with `ref`, `out`, `in` or `params` parameters, and methods in generated code. The fix inlines an expression body (or a lone `return`) at the invocation with each parameter replaced by its argument, or splices a void or Task block body in place of the call statement with one `var` local per parameter; it is only offered when no name in the body is captured or shadowed at the call site, no argument is dropped or repeated unless it is side-effect free, no conversion changes, and the call site is in the same file as the method. Otherwise the diagnostic stands without a fix.

## ECO006: OneOf failure should be handled first

A OneOf result is checked for failure first with an early return, so the happy path continues un-nested instead of sitting inside the success branch.

```csharp
// Wrong
if (result.TryPickT0(out var page, out var failed))
{
    _rows.AddRange(page);
    _hasMore = page.Count == PageSize;
}
else
{
    Snackbar.Add(FailureMessage(failed), Severity.Error);
    return;
}

// Right
if (!result.TryPickT0(out var page, out var failed))
{
    Snackbar.Add(FailureMessage(failed), Severity.Error);
    return;
}

_rows.AddRange(page);
_hasMore = page.Count == PageSize;
```

Flagged: an `if` whose condition is `IsT0` or a `TryPickT0(...)` call (checked structurally, on any receiver) when its `else` ends in a jump (return, throw, continue, break), or when it has no `else`, is the last statement of its block, and nests two or more statements, so the failure is silently dropped. Not flagged: negated conditions (`!result.IsT0`, `!result.TryPickT0(...)`), `IsT1` and `TryPickT1`, a then body that itself ends in a jump (an early-out on success), `else if` chains, a single-statement body without `else`, and generated code. The fix negates the condition, moves the failure branch into the `if`, and emits the old success statements after it at the same indentation with their comments intact; without an `else` the failure branch becomes a bare `return;`, so that fix is only offered inside a void, async `Task`, or async `ValueTask` method, local function, or lambda where the `if` closes every block up to the member body (not under a loop, switch section, or finally).

## ECO007: Type name should not end in a generic suffix

A class, struct, record, record struct or interface is named for the responsibility it owns. A name ending in `Service`, `Manager`, `Helper`, `Helpers`, `Utils`, `Utilities` or `Common` (case-sensitive, including a name that is exactly the suffix and an interface name after its leading `I`) says nothing about that responsibility and tends to collect unrelated members.

```csharp
// Wrong
public sealed class UserLookupService
{
    public Task<User?> FindByEmail(string email, CancellationToken cancellationToken) { ... }
}

// Right
public sealed class UserLookup
{
    public Task<User?> FindByEmail(string email, CancellationToken cancellationToken) { ... }
}
```

Flagged: nested types, generic types (`CacheService<T>`), every part of a partial type, and subclasses of framework types that carry the suffix themselves (`SpaNavigationManager : NavigationManager`); the base type is never an excuse. Not flagged: enums, delegates, implicitly declared types, generated code, lowercase matches such as `Microservice`, and names where the word sits anywhere but the end (`ServiceRegistration`). There is no code fix: a fix cannot invent a better name.

## ECO008: Contract record should carry only data

A record in a `Contracts` or `Dtos` namespace moves between components as plain data. Derived values such as `IsEmpty`, `ActiveCount` or `DisplayName` are computed by the component that needs them, not baked onto the record.

```csharp
// Wrong
public sealed record SpeciesDto(string Name, int Count)
{
    public bool IsEmpty => Count == 0;
}

// Right
public sealed record SpeciesDto(string Name, int Count);

// In the consumer
var isEmpty = species.Count == 0;
```

Flagged: every instance member of a record class or record struct that carries logic, when any segment of the containing namespace is `Contracts`, `Contract`, `Dto` or `Dtos`: ordinary methods (including `ToString`, `Equals` and `GetHashCode` overrides and explicit interface implementations), properties with an expression body or a non-auto accessor (including `init` with a body), and indexers. Nested records inside such a record qualify too. Not flagged: positional parameters, auto-properties (with or without initializers, `required` or not), fields, constructors, static members of any kind (static factories such as `RequestFailed.From` are allowed), operators and conversion operators, `Deconstruct`, nested type declarations themselves, plain classes and structs, records outside those namespaces, and generated code. There is no fix: moving logic to the consumer is a design change.

## ECO009: Warning suppression needs a justification

A warning is fixed, not silenced. The rare suppression states why the warning does not apply, so the reason is reviewed with the code: a `#pragma warning disable` carries it in a trailing comment on the same line, and a `SuppressMessage` or `UnconditionalSuppressMessage` attribute carries it in a non-blank `Justification`.

```csharp
// Wrong
#pragma warning disable CS0618
[SuppressMessage("Trimming", "IL2026")]

// Right
#pragma warning disable CS0618 // Legacy client still needs the obsolete overload
[SuppressMessage("Trimming", "IL2026", Justification = "The member is preserved by the descriptor")]
```

Not flagged: `#pragma warning restore`, a disable directive with any non-blank trailing comment, attributes whose `Justification` is a non-blank constant, same-named attributes from other namespaces, and generated code. A disable directive with no id list is reported as disabling all warnings. Assembly and module level attributes are included. There is no code fix: a fix cannot invent the reason.

## ECO010: Razor markup should not use inline styles

Blazor markup never carries a `style` attribute or a `<style>` element. Every visual rule lives in a stylesheet, the component's `.razor.css` or the app's, including sizes that a JavaScript library needs on its host element. The Razor SDK already hands every `.razor` file to analyzers as an additional file, so no extra wiring is needed.

```razor
@* Wrong *@
<div @ref="_host" style="height: 400px; width: 100%"></div>
<span style="background: var(--accent)">@Label</span>

@* Right *@
<div @ref="_host" class="map-host"></div>
<span class="badge badge-accent">@Label</span>
```

Flagged: every `style` attribute in a `.razor` file, whether the value is a literal, a bound value (`style="@Style"`, `style="width:@Width"`), or a component `Style` parameter, and every `<style>` element, reported once at its opening tag. Not flagged: the CSS inside a `<style>` element (the element itself already is), attributes that merely end in `style` such as `data-style`, text inside Razor comments (`@* ... *@`), identifiers inside `@code` and `@functions` blocks, and `.cshtml` files. There is no fix: the right class name and the stylesheet it belongs in are design decisions.

## Adding a new rule

1. Analyzer class in `Rules/` with the next `ECO00X` id, concurrent execution enabled, generated code excluded.
2. Code fix in `src/Analyzers/EcoData.Analyzers.CodeFixes` whenever an automatic repair is safe.
3. Row in `AnalyzerReleases.Unshipped.md`.
4. At least 10 tests per analyzer in `tests/EcoData.Analyzers.Tests`, covering the fix output and the deliberate exclusions.
5. Document the rule in this file.
