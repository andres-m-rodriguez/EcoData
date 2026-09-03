# EcoData.Analyzers

Code style conventions for the EcoData solution. Rules are enforced as Roslyn analyzers where possible; the rest are written conventions checked in review.

## Layout and wiring

- Analyzer classes live in `Rules/`, one file per rule.
- Code fixes live in `src/Analyzers/EcoData.Analyzers.CodeFixes` (a separate assembly; the compiler cannot load Workspaces-dependent code).
- Tests live in `tests/EcoData.Analyzers.Tests`.
- The root `Directory.Build.targets` references both analyzer projects with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"` from every project, so the rules run on every build. A project opts out only with `UseEcoDataAnalyzers=false`. Builds must stay at zero warnings.

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

## Adding a new rule

1. Analyzer class in `Rules/` with the next `ECO00X` id, concurrent execution enabled, generated code excluded.
2. Code fix in `src/Analyzers/EcoData.Analyzers.CodeFixes` whenever an automatic repair is safe.
3. Row in `AnalyzerReleases.Unshipped.md`.
4. At least 10 tests per analyzer in `tests/EcoData.Analyzers.Tests`, covering the fix output and the deliberate exclusions.
5. Document the rule in this file.
